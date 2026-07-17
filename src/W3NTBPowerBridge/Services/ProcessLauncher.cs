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

    /// <inheritdoc />
    public void LaunchWfviewServer(AppSettings settings)
    {
        LaunchCommand(settings.WfviewServerLaunchCommand, settings.WfviewServerLaunchArguments, "wfview server");
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

    private void LaunchCommand(string command, string arguments, string name)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            _logger.Info($"{name} launch command is not configured.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(command, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            _logger.Info($"Launched {name} command.");
        }
        catch (Exception exception)
        {
            _logger.Error($"Failed to launch {name} command", exception);
        }
    }
}
