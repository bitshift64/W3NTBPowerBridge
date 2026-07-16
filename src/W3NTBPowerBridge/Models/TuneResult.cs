namespace W3NTBPowerBridge.Models;

/// <summary>
/// Represents the result of one rigctld tune command and confirmation query.
/// </summary>
/// <param name="RequestedHz">The requested frequency in Hz.</param>
/// <param name="ConfirmedHz">The confirmed frequency in Hz, when available.</param>
/// <param name="Succeeded">True when the confirmed frequency is within tolerance.</param>
/// <param name="Message">Human-readable result text.</param>
public sealed record TuneResult(long RequestedHz, long? ConfirmedHz, bool Succeeded, string Message);
