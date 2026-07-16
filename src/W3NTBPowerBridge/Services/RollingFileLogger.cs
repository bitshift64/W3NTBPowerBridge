using System.IO;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Writes events to a rolling log file in the user's AppData folder.
/// </summary>
public sealed class RollingFileLogger : IAppLogger
{
    private const long MaxBytes = 1_000_000;
    private const int MaxArchives = 5;
    private readonly object _syncRoot = new();
    private readonly string _logPath;

    /// <inheritdoc />
    public event EventHandler<string>? LineWritten;

    /// <summary>
    /// Creates a rolling file logger.
    /// </summary>
    public RollingFileLogger()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "W3NTBPowerBridge", "Logs");
        Directory.CreateDirectory(directory);
        _logPath = Path.Combine(directory, "app.log");
    }

    /// <inheritdoc />
    public void Info(string message)
    {
        Write("INFO", message);
    }

    /// <inheritdoc />
    public void Error(string message, Exception? exception = null)
    {
        Write("ERROR", exception is null ? message : $"{message}: {exception.Message}");
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff zzz} [{level}] {message}";
        lock (_syncRoot)
        {
            RollIfNeeded();
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }

        LineWritten?.Invoke(this, line);
    }

    private void RollIfNeeded()
    {
        var info = new FileInfo(_logPath);
        if (!info.Exists || info.Length < MaxBytes)
        {
            return;
        }

        for (var index = MaxArchives - 1; index >= 1; index--)
        {
            var source = $"{_logPath}.{index}";
            var target = $"{_logPath}.{index + 1}";
            if (File.Exists(target))
            {
                File.Delete(target);
            }

            if (File.Exists(source))
            {
                File.Move(source, target);
            }
        }

        File.Move(_logPath, $"{_logPath}.1", true);
    }
}
