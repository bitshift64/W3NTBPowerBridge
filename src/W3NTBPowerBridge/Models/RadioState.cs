namespace W3NTBPowerBridge.Models;

/// <summary>
/// Represents the current radio state as reported by wfview rigctld.
/// </summary>
/// <param name="FrequencyHz">Current radio frequency in Hz.</param>
/// <param name="Mode">Current radio mode, when available.</param>
/// <param name="TxPowerPercent">Current RF power level as a percentage, when available.</param>
/// <param name="IsTransmitting">True when wfview reports PTT/transmit is active, false when it reports receive, or null when unavailable.</param>
public sealed record RadioState(long? FrequencyHz, string? Mode, double? TxPowerPercent, bool? IsTransmitting);
