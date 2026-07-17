using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using W3NTBPowerBridge.Models;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Talks to a Shelly Gen 4 plug through the local HTTP RPC API.
/// </summary>
public sealed class ShellyPowerService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(3)
    };

    private readonly IAppLogger _logger;

    /// <summary>
    /// Creates the Shelly power service.
    /// </summary>
    /// <param name="logger">Application logger.</param>
    public ShellyPowerService(IAppLogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Reads the Shelly switch status.
    /// </summary>
    /// <param name="settings">Current application settings.</param>
    /// <returns>The latest Shelly status.</returns>
    public async Task<ShellyPowerStatus> GetStatusAsync(AppSettings settings)
    {
        if (!settings.ShellyEnabled || string.IsNullOrWhiteSpace(settings.ShellyHost))
        {
            return ShellyPowerStatus.Disabled;
        }

        try
        {
            var uri = BuildUri(settings, "Switch.GetStatus", $"id={settings.ShellySwitchId}");
            var json = await HttpClient.GetStringAsync(uri).ConfigureAwait(false);
            return ParseStatus(json, settings.StationOffPowerThresholdWatts);
        }
        catch (Exception exception)
        {
            _logger.Error("Shelly status query failed", exception);
            return new ShellyPowerStatus(true, false, null, null, null, null, null, false, exception.Message);
        }
    }

    /// <summary>
    /// Turns the Shelly relay output on or off.
    /// </summary>
    /// <param name="settings">Current application settings.</param>
    /// <param name="turnOn">True to turn station power on; false to turn it off.</param>
    /// <returns>The status after the command.</returns>
    public async Task<ShellyPowerStatus> SetPowerAsync(AppSettings settings, bool turnOn)
    {
        if (!settings.ShellyEnabled || string.IsNullOrWhiteSpace(settings.ShellyHost))
        {
            return ShellyPowerStatus.Disabled;
        }

        var commandName = turnOn ? "ON" : "OFF";
        try
        {
            var uri = BuildUri(settings, "Switch.Set", $"id={settings.ShellySwitchId}&on={turnOn.ToString().ToLowerInvariant()}");
            _ = await HttpClient.GetStringAsync(uri).ConfigureAwait(false);
            _logger.Info($"Shelly station power command sent: {commandName}.");

            return await WaitForOutputStateAsync(settings, turnOn).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error($"Shelly station power command failed: {commandName}", exception);
            return new ShellyPowerStatus(true, false, null, null, null, null, null, false, exception.Message);
        }
    }

    /// <summary>
    /// Parses a Shelly Switch.GetStatus JSON response.
    /// </summary>
    /// <param name="json">Shelly JSON response.</param>
    /// <param name="offThresholdWatts">Wattage below which station off is confirmed.</param>
    /// <returns>A parsed Shelly status.</returns>
    public static ShellyPowerStatus ParseStatus(string json, double offThresholdWatts)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.TryGetProperty("params", out var parameters) && parameters.ValueKind == JsonValueKind.Object)
        {
            root = parameters;
        }

        var outputOn = ReadBoolean(root, "output");
        var powerWatts = ReadDouble(root, "apower");
        var voltage = ReadDouble(root, "voltage");
        var current = ReadDouble(root, "current");
        var frequency = ReadDouble(root, "freq");
        var stationOffConfirmed = outputOn == false || (powerWatts.HasValue && powerWatts.Value <= offThresholdWatts);
        var message = outputOn == true ? "Station power is on" : "Station power is off";

        if (outputOn == true && stationOffConfirmed)
        {
            message = $"Station off confirmed by low power ({powerWatts:0.0} W)";
        }

        return new ShellyPowerStatus(true, true, outputOn, powerWatts, voltage, current, frequency, stationOffConfirmed, message);
    }

    private async Task<ShellyPowerStatus> WaitForOutputStateAsync(AppSettings settings, bool expectedOn)
    {
        ShellyPowerStatus status = ShellyPowerStatus.Disabled;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            status = await GetStatusAsync(settings).ConfigureAwait(false);
            if (status.OutputOn == expectedOn)
            {
                return status;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);
        }

        var expected = expectedOn ? "on" : "off";
        var message = $"Shelly command sent, but relay did not confirm {expected}. Last status: {status.Message}";
        _logger.Error(message);

        return status with { Message = message };
    }

    private static Uri BuildUri(AppSettings settings, string method, string query)
    {
        var host = settings.ShellyHost.Trim();
        var scheme = "http";
        var port = settings.ShellyPort;

        if (Uri.TryCreate(host, UriKind.Absolute, out var configuredUri))
        {
            scheme = configuredUri.Scheme;
            host = configuredUri.Host;
            port = configuredUri.IsDefaultPort ? settings.ShellyPort : configuredUri.Port;
        }

        var builder = new UriBuilder(scheme, host, port)
        {
            Path = $"/rpc/{method}",
            Query = query
        };

        return builder.Uri;
    }

    private static bool? ReadBoolean(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;
    }

    private static double? ReadDouble(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDouble(out var number) => number,
            JsonValueKind.String when double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number,
            _ => null
        };
    }
}
