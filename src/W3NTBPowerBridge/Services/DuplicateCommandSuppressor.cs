namespace W3NTBPowerBridge.Services;

/// <summary>
/// Suppresses duplicate frequency commands that arrive inside a short time window.
/// </summary>
public sealed class DuplicateCommandSuppressor
{
    private readonly TimeSpan _window;
    private long? _lastFrequencyHz;
    private DateTimeOffset _lastAcceptedAt = DateTimeOffset.MinValue;

    /// <summary>
    /// Creates a duplicate command suppressor.
    /// </summary>
    /// <param name="window">The duplicate suppression window.</param>
    public DuplicateCommandSuppressor(TimeSpan window)
    {
        _window = window;
    }

    /// <summary>
    /// Returns true when a command should be suppressed as a duplicate.
    /// </summary>
    /// <param name="frequencyHz">Requested frequency in Hz.</param>
    /// <param name="now">Current time used for comparison.</param>
    /// <returns>True when the frequency duplicates the last accepted command inside the window.</returns>
    public bool ShouldSuppress(long frequencyHz, DateTimeOffset now)
    {
        if (_lastFrequencyHz == frequencyHz && now - _lastAcceptedAt < _window)
        {
            return true;
        }

        _lastFrequencyHz = frequencyHz;
        _lastAcceptedAt = now;
        return false;
    }
}
