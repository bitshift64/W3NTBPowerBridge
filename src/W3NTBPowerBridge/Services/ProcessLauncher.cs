using System.Diagnostics;
using System.IO;
using W3NTBPowerBridge.Models;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Starts external station applications without requiring administrator rights.
/// </summary>
public sealed class ProcessLauncher : IProcessLauncher
{
    private readonly IAppLogger _logger;

    /// <summary>
    /// Creates a process launcher.
    /// </summary>
    /// <param name="logger">Application logger.</param>
    public ProcessLauncher(IAppLogger logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void LaunchWfview(AppSettings settings)
    {
        Launch(settings.WfviewPath, "wfview");
    }

    /// <inheritdoc />
    public void LaunchAcLog(AppSettings settings)
    {
        Launch(settings.AcLogPath, "ACLog");
    }

    private void Launch(string path, string name)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.Info($"{name} path is not configured.");
            return;
        }

        if (!File.Exists(path))
        {
            _logger.Error($"{name} executable was not found at {path}");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            _logger.Info($"Launched {name}.");
        }
        catch (Exception exception)
        {
            _logger.Error($"Failed to launch {name}", exception);
        }
    }
}
