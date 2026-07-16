namespace W3NTBPowerBridge.Models;

/// <summary>
/// Represents an ACLog request to change the radio frequency.
/// </summary>
/// <param name="FrequencyHz">Requested frequency in Hz.</param>
/// <param name="OriginalValue">Original frequency text received from ACLog.</param>
/// <param name="Mode">Optional mode received near the frequency request.</param>
/// <param name="Source">The ACLog API source for the request.</param>
public sealed record FrequencyRequest(long FrequencyHz, string OriginalValue, string? Mode = null, string Source = "CHANGEFREQ");
