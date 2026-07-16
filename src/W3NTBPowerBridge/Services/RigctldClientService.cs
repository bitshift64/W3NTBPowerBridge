using System.Net.Sockets;
using System.Text;
using W3NTBPowerBridge.Models;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Maintains an asynchronous TCP connection to wfview's Hamlib rigctld server.
/// </summary>
public sealed class RigctldClientService
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(3);
    private readonly IAppLogger _logger;
    private readonly SemaphoreSlim _commandLock = new(1, 1);
    private CancellationTokenSource? _runCts;
    private TcpClient? _client;
    private RadioState _lastState = new(null, null, null, null);

    /// <summary>
    /// Raised when the connection state changes.
    /// </summary>
    public event EventHandler<ServiceConnectionState>? StatusChanged;

    /// <summary>
    /// Raised when wfview reports a changed radio state.
    /// </summary>
    public event EventHandler<RadioState>? RadioStateChanged;

    /// <summary>
    /// Creates the rigctld client service.
    /// </summary>
    /// <param name="logger">Application logger.</param>
    public RigctldClientService(IAppLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts the background connection loop.
    /// </summary>
    /// <param name="settings">Current application settings.</param>
    public void Start(AppSettings settings)
    {
        Stop();
        _runCts = new CancellationTokenSource();
        _ = RunAsync(settings, _runCts.Token);
    }

    /// <summary>
    /// Stops the background connection loop.
    /// </summary>
    public void Stop()
    {
        _runCts?.Cancel();
        _runCts = null;
        _client?.Close();
        _client = null;
        StatusChanged?.Invoke(this, ServiceConnectionState.Disconnected);
    }

    /// <summary>
    /// Sends a frequency command and confirms the radio frequency.
    /// </summary>
    /// <param name="frequencyHz">Requested frequency in Hz.</param>
    /// <returns>The tune result.</returns>
    public async Task<TuneResult> TuneAsync(long frequencyHz)
    {
        await _commandLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var client = _client;
            if (client is null || !client.Connected)
            {
                return new TuneResult(frequencyHz, null, false, "wfview is not connected.");
            }

            using var timeout = new CancellationTokenSource(CommandTimeout);
            await SendCommandNoLockAsync(client, $"F {frequencyHz}", timeout.Token).ConfigureAwait(false);
            _logger.Info($"Sent Hamlib frequency command: F {frequencyHz}");
            var response = await SendCommandForFrequencyNoLockAsync(client, "f", timeout.Token).ConfigureAwait(false);

            if (!HamlibResponseParser.TryParseFrequency(response, out var confirmedHz))
            {
                return new TuneResult(frequencyHz, null, false, "No valid frequency confirmation was returned.");
            }

            var succeeded = Math.Abs(confirmedHz - frequencyHz) <= 10;
            var message = succeeded ? "Tune confirmed." : $"Tune mismatch: radio reported {confirmedHz} Hz.";
            return new TuneResult(frequencyHz, confirmedHz, succeeded, message);
        }
        catch (Exception exception)
        {
            _logger.Error("rigctld tune command failed", exception);
            _client?.Close();
            return new TuneResult(frequencyHz, null, false, exception.Message);
        }
        finally
        {
            _commandLock.Release();
        }
    }

    /// <summary>
    /// Sends a Hamlib mode command to wfview.
    /// </summary>
    /// <param name="mode">Rig mode such as CW, USB, LSB, AM, FM, or RTTY.</param>
    /// <returns>A task representing the mode command.</returns>
    public async Task SetModeAsync(string mode)
    {
        await _commandLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var client = _client;
            if (client is null || !client.Connected || string.IsNullOrWhiteSpace(mode))
            {
                return;
            }

            using var timeout = new CancellationTokenSource(CommandTimeout);
            await SendCommandNoLockAsync(client, $"M {mode.ToUpperInvariant()} 0", timeout.Token).ConfigureAwait(false);
            _logger.Info($"Sent Hamlib mode command: M {mode.ToUpperInvariant()} 0");
        }
        catch (Exception exception)
        {
            _logger.Error("rigctld mode command failed", exception);
            _client?.Close();
        }
        finally
        {
            _commandLock.Release();
        }
    }

    /// <summary>
    /// Queries wfview for the current radio state.
    /// </summary>
    /// <returns>The current radio state.</returns>
    public async Task<RadioState> QueryRadioStateAsync()
    {
        await _commandLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var client = _client;
            if (client is null || !client.Connected)
            {
                return new RadioState(null, null, null, null);
            }

            using var timeout = new CancellationTokenSource(CommandTimeout);
            var frequency = await QueryFrequencyNoLockAsync(client, timeout.Token).ConfigureAwait(false);
            var mode = await QueryModeNoLockAsync(client, timeout.Token).ConfigureAwait(false);
            var power = await QueryPowerNoLockAsync(client, timeout.Token).ConfigureAwait(false);
            var isTransmitting = await QueryPttNoLockAsync(client, timeout.Token).ConfigureAwait(false);
            return new RadioState(frequency, mode, power, isTransmitting);
        }
        catch (Exception exception)
        {
            _logger.Error("rigctld state query failed", exception);
            _client?.Close();
            return new RadioState(null, null, null, null);
        }
        finally
        {
            _commandLock.Release();
        }
    }

    private async Task RunAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                StatusChanged?.Invoke(this, ServiceConnectionState.Connecting);
                var client = new TcpClient();
                await client.ConnectAsync(settings.WfviewHost, settings.WfviewPort, cancellationToken).ConfigureAwait(false);
                _client = client;
                StatusChanged?.Invoke(this, ServiceConnectionState.Connected);
                _logger.Info($"Connected to wfview rigctld at {settings.WfviewHost}:{settings.WfviewPort}.");

                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    var state = await QueryRadioStateAsync().ConfigureAwait(false);
                    PublishStateIfChanged(state);
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.Error("wfview rigctld connection error", exception);
            }

            _client?.Close();
            _client = null;
            StatusChanged?.Invoke(this, ServiceConnectionState.Waiting);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        StatusChanged?.Invoke(this, ServiceConnectionState.Disconnected);
        _logger.Info("wfview rigctld connection stopped.");
    }

    private static async Task SendLineAsync(NetworkStream stream, string command, CancellationToken cancellationToken)
    {
        var bytes = Encoding.ASCII.GetBytes(command + "\n");
        await stream.WriteAsync(bytes.AsMemory(0, bytes.Length), cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> ReadResponseAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        var response = new StringBuilder();
        var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
        response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));

        await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken).ConfigureAwait(false);
        while (stream.DataAvailable)
        {
            bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            response.Append(Encoding.ASCII.GetString(buffer, 0, bytesRead));
            await Task.Delay(TimeSpan.FromMilliseconds(5), cancellationToken).ConfigureAwait(false);
        }

        return response.ToString();
    }

    private static async Task<string> ReadUntilFrequencyAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var response = new StringBuilder();
        while (!cancellationToken.IsCancellationRequested)
        {
            response.Append(await ReadResponseAsync(stream, cancellationToken).ConfigureAwait(false));
            if (HamlibResponseParser.TryParseFrequency(response.ToString(), out _))
            {
                return response.ToString();
            }
        }

        return response.ToString();
    }

    private async Task SendCommandNoLockAsync(TcpClient client, string command, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();
        await SendLineAsync(stream, command, cancellationToken).ConfigureAwait(false);
        _ = await ReadResponseAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> SendCommandForFrequencyNoLockAsync(TcpClient client, string command, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();
        await SendLineAsync(stream, command, cancellationToken).ConfigureAwait(false);
        return await ReadUntilFrequencyAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> SendCommandForResponseNoLockAsync(TcpClient client, string command, CancellationToken cancellationToken)
    {
        var stream = client.GetStream();
        await SendLineAsync(stream, command, cancellationToken).ConfigureAwait(false);
        return await ReadResponseAsync(stream, cancellationToken).ConfigureAwait(false);
    }

    private async Task<long?> QueryFrequencyNoLockAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var response = await SendCommandForFrequencyNoLockAsync(client, "f", cancellationToken).ConfigureAwait(false);
        return HamlibResponseParser.TryParseFrequency(response, out var frequencyHz) ? frequencyHz : null;
    }

    private async Task<string?> QueryModeNoLockAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var response = await SendCommandForResponseNoLockAsync(client, "m", cancellationToken).ConfigureAwait(false);
        return HamlibResponseParser.TryParseMode(response, out var mode) ? mode : null;
    }

    private async Task<double?> QueryPowerNoLockAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var response = await SendCommandForResponseNoLockAsync(client, "l RFPOWER", cancellationToken).ConfigureAwait(false);
        if (!HamlibResponseParser.TryParseLevel(response, out var power))
        {
            return null;
        }

        return power <= 1 ? Math.Round(power * 100, 1) : Math.Round(power, 1);
    }

    private async Task<bool?> QueryPttNoLockAsync(TcpClient client, CancellationToken cancellationToken)
    {
        var response = await SendCommandForResponseNoLockAsync(client, "t", cancellationToken).ConfigureAwait(false);
        return HamlibResponseParser.TryParsePtt(response, out var isTransmitting) ? isTransmitting : null;
    }

    private void PublishStateIfChanged(RadioState state)
    {
        if (state == _lastState)
        {
            return;
        }

        _lastState = state;
        RadioStateChanged?.Invoke(this, state);
    }
}
