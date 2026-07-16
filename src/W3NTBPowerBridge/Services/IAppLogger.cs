namespace W3NTBPowerBridge.Services;

/// <summary>
/// Writes application events to durable storage and notifies the user interface.
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// Raised when a new log line is written.
    /// </summary>
    event EventHandler<string>? LineWritten;

    /// <summary>
    /// Writes an informational line.
    /// </summary>
    /// <param name="message">Message to write.</param>
    void Info(string message);

    /// <summary>
    /// Writes an error line.
    /// </summary>
    /// <param name="message">Message to write.</param>
    /// <param name="exception">Optional exception details.</param>
    void Error(string message, Exception? exception = null);
}
