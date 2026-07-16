namespace W3NTBPowerBridge.Models;

/// <summary>
/// Represents the local RPC status of a Shelly switch component.
/// </summary>
/// <param name="IsEnabled">True when Shelly integration is enabled in settings.</param>
/// <param name="IsReachable">True when the Shelly device responded.</param>
/// <param name="OutputOn">True when the Shelly relay output is on.</param>
/// <param name="PowerWatts">Current active power in watts.</param>
/// <param name="Voltage">Measured AC voltage.</param>
/// <param name="CurrentAmps">Measured AC current in amps.</param>
/// <param name="FrequencyHz">Measured AC line frequency in Hz.</param>
/// <param name="StationOffConfirmed">True when relay is off or wattage is below the configured off threshold.</param>
/// <param name="Message">Human-readable status message.</param>
public sealed record ShellyPowerStatus(
    bool IsEnabled,
    bool IsReachable,
    bool? OutputOn,
    double? PowerWatts,
    double? Voltage,
    double? CurrentAmps,
    double? FrequencyHz,
    bool StationOffConfirmed,
    string Message)
{
    /// <summary>
    /// Gets a disabled Shelly status.
    /// </summary>
    public static ShellyPowerStatus Disabled { get; } = new(false, false, null, null, null, null, null, false, "Shelly disabled");
}
