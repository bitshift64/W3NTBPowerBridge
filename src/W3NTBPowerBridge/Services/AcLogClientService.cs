using System.IO;
using System.Net.Sockets;
using System.Text;
using W3NTBPowerBridge.Models;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Maintains an asynchronous TCP connection to N3FJP Amateur Contact Log.
/// </summary>
public sealed class AcLogClientService
{
    private readonly IAppLogger _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private TcpClient? _client;
    private string? _lastSentFrequencyMhz;
    private string? _lastSentMode;
    private DateTimeOffset _ignoreEchoUntil = DateTimeOffset.MinValue;

    /// <summary>
    /// Raised when the connection state changes.
    /// </summary>
    public event EventHandler<ServiceConnectionState>? StatusChanged;

    /// <summary>
    /// Raised when ACLog sends a valid CHANGEFREQ request.
    /// </summary>
    public event EventHandler<FrequencyRequest>? FrequencyRequested;

    /// <summary>
    /// Raised when ACLog sends a mode field update.
    /// </summary>
    public event EventHandler<string>? ModeRequested;

    /// <summary>
    /// Creates the ACLog client service.
    /// </summary>
    /// <param name="logger">Application logger.</param>
    public AcLogClientService(IAppLogger logger)
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
        _runTask = RunAsync(settings, _runCts.Token);
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
    /// Sends the current radio state to ACLog's band, mode, and frequency fields.
    /// </summary>
    /// <param name="state">Current radio state from wfview.</param>
    /// <returns>A task representing the update operation.</returns>
    public async Task UpdateRadioStateAsync(RadioState state)
    {
        var client = _client;
        if (client is null || !client.Connected || state.FrequencyHz is null)
        {
            return;
        }

        var band = FrequencyConverter.HzToBand(state.FrequencyHz.Value);
        var frequencyMhz = FrequencyConverter.FormatHzAsAcLogMhz(state.FrequencyHz.Value);
        var mode = NormalizeModeForAcLog(state.Mode);
        _lastSentFrequencyMhz = frequencyMhz;
        _lastSentMode = mode;
        _ignoreEchoUntil = DateTimeOffset.Now.AddSeconds(2);
        var commands = new List<string>
        {
            "<CMD><IGNORERIGPOLLS><VALUE>TRUE</VALUE></CMD>"
        };

        if (!string.IsNullOrWhiteSpace(band) || !string.IsNullOrWhiteSpace(mode))
        {
            commands.Add($"<CMD><CHANGEBM><BAND>{EscapeXml(band)}</BAND><MODE>{EscapeXml(mode)}</MODE></CMD>");
        }

        commands.Add($"<CMD><UPDATE><CONTROL>TXTENTRYFREQUENCY</CONTROL><VALUE>{EscapeXml(frequencyMhz)}</VALUE></CMD>");
        if (!string.IsNullOrWhiteSpace(mode))
        {
            commands.Add($"<CMD><UPDATE><CONTROL>TXTENTRYMODE</CONTROL><VALUE>{EscapeXml(mode)}</VALUE></CMD>");
        }

        commands.Add("<CMD><IGNORERIGPOLLS><VALUE>FALSE</VALUE></CMD>");

        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var stream = client.GetStream();
            foreach (var command in commands)
            {
                var bytes = Encoding.UTF8.GetBytes(command + "\r\n");
                await stream.WriteAsync(bytes).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMilliseconds(10)).ConfigureAwait(false);
            }

            _logger.Info($"Updated ACLog from wfview: {frequencyMhz} MHz, band {band}, mode {mode}.");
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to update ACLog radio state", exception);
            _client?.Close();
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task RunAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                StatusChanged?.Invoke(this, ServiceConnectionState.Connecting);
                using var client = new TcpClient();
                await client.ConnectAsync(settings.AcLogHost, settings.AcLogPort, cancellationToken).ConfigureAwait(false);
                _client = client;
                StatusChanged?.Invoke(this, ServiceConnectionState.Connected);
                _logger.Info($"Connected to ACLog at {settings.AcLogHost}:{settings.AcLogPort}.");
                await SendInitialCommandsAsync(client, cancellationToken).ConfigureAwait(false);
                await ReadLoopAsync(client.GetStream(), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.Error("ACLog connection error", exception);
            }

            _client = null;
            StatusChanged?.Invoke(this, ServiceConnectionState.Waiting);
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        StatusChanged?.Invoke(this, ServiceConnectionState.Disconnected);
        _logger.Info("ACLog connection stopped.");
    }

    private async Task ReadLoopAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var pending = new StringBuilder();

        while (!cancellationToken.IsCancellationRequested)
        {
            var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (bytesRead == 0)
            {
                throw new IOException("ACLog disconnected.");
            }

            pending.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
            foreach (var message in ExtractMessages(pending))
            {
                if (AcLogMessageParser.TryParseChangeFrequency(message, out var request))
                {
                    _logger.Info($"ACLog CHANGEFREQ request received: {request.OriginalValue} MHz ({request.FrequencyHz} Hz).");
                    FrequencyRequested?.Invoke(this, request);
                }
                else if (AcLogMessageParser.TryParseReadBmfResponse(message, out request))
                {
                    _logger.Info($"ACLog READBMF response received: {request.OriginalValue} MHz ({request.FrequencyHz} Hz), mode {request.Mode ?? "unknown"}.");
                    FrequencyRequested?.Invoke(this, request);
                }
                else if (AcLogMessageParser.TryParseModeUpdate(message, out var mode))
                {
                    if (!ShouldIgnoreModeEcho(mode))
                    {
                        _logger.Info($"ACLog mode request received from field update: {mode}.");
                        ModeRequested?.Invoke(this, mode);
                    }
                }
                else if (AcLogMessageParser.TryParseFrequencyUpdate(message, out request))
                {
                    if (!ShouldIgnoreFrequencyEcho(request.OriginalValue))
                    {
                        _logger.Info($"ACLog frequency request received from field update: {request.OriginalValue} MHz ({request.FrequencyHz} Hz).");
                        FrequencyRequested?.Invoke(this, request);
                    }
                }
                else
                {
                    _logger.Info($"Ignored ACLog message that was not a valid CHANGEFREQ request: {message}");
                }
            }
        }
    }

    private static IEnumerable<string> ExtractMessages(StringBuilder pending)
    {
        var text = pending.ToString();
        var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        if (lines.Length > 1)
        {
            pending.Clear();
            pending.Append(lines[^1]);
            return lines.Take(lines.Length - 1).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        }

        if (text.TrimEnd().EndsWith('>') && AcLogMessageParser.TryParseChangeFrequency(text, out _))
        {
            pending.Clear();
            return new[] { text };
        }

        if (pending.Length > 64_000)
        {
            pending.Clear();
        }

        return Array.Empty<string>();
    }

    private async Task SendInitialCommandsAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var stream = client.GetStream();
            foreach (var command in new[] { "<CMD><SETUPDATESTATE><VALUE>TRUE</VALUE></CMD>", "<CMD><READBMF></CMD>" })
            {
                var bytes = Encoding.UTF8.GetBytes(command + "\r\n");
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMilliseconds(10), cancellationToken).ConfigureAwait(false);
            }

            _logger.Info("Requested ACLog update notifications and current band/mode/frequency.");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static string NormalizeModeForAcLog(string? mode)
    {
        return mode?.ToUpperInvariant() switch
        {
            "PKTUSB" => "FT8",
            "PKTLSB" => "FT8",
            "DATA-U" => "FT8",
            "DATA-L" => "FT8",
            null => string.Empty,
            var value => value
        };
    }

    private static string EscapeXml(string value)
    {
        return System.Security.SecurityElement.Escape(value) ?? string.Empty;
    }

    private bool ShouldIgnoreFrequencyEcho(string frequencyMhz)
    {
        return DateTimeOffset.Now <= _ignoreEchoUntil
            && !string.IsNullOrWhiteSpace(_lastSentFrequencyMhz)
            && decimal.TryParse(frequencyMhz, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var incoming)
            && decimal.TryParse(_lastSentFrequencyMhz, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var sent)
            && Math.Abs(incoming - sent) < 0.000001m;
    }

    private bool ShouldIgnoreModeEcho(string mode)
    {
        return DateTimeOffset.Now <= _ignoreEchoUntil
            && string.Equals(mode, _lastSentMode, StringComparison.OrdinalIgnoreCase);
    }
}
