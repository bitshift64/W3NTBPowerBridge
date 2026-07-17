using System.Diagnostics;
using System.IO;
using System.Text;
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
        LaunchWfviewServerOverSsh(settings.WfviewServerHost, settings.WfviewServerPath);
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

    private void LaunchWfviewServerOverSsh(string host, string wfviewPath)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.Info("wfview server host is not configured.");
            return;
        }

        var path = string.IsNullOrWhiteSpace(wfviewPath) ? @"C:\Program Files\wfview\wfview.exe" : wfviewPath;
        var escapedPath = path.Replace("'", "''");
        var script = $"""
            $taskName = 'W3NTB Launch wfview'
            $wfviewPath = '{escapedPath}'
            $userId = (whoami).Trim()
            Stop-Process -Name wfview -Force -ErrorAction SilentlyContinue
            $action = New-ScheduledTaskAction -Execute $wfviewPath
            $principal = New-ScheduledTaskPrincipal -UserId $userId -LogonType Interactive -RunLevel Limited
            Register-ScheduledTask -TaskName $taskName -Action $action -Principal $principal -Force | Out-Null
            Start-ScheduledTask -TaskName $taskName
            Start-Sleep -Seconds 3
            Get-Process -Name wfview -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Id
            """;
        var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        try
        {
            var process = Process.Start(new ProcessStartInfo("ssh.exe", $"{host} powershell.exe -NoProfile -EncodedCommand {encodedCommand}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process is null)
            {
                _logger.Error("Failed to start SSH process for wfview server restart.");
                return;
            }

            _ = LogServerLaunchResultAsync(process, host);
            _logger.Info($"Requested wfview server restart on {host}.");
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to launch wfview server restart command", exception);
        }
    }

    private async Task LogServerLaunchResultAsync(Process process, string host)
    {
        try
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().ConfigureAwait(false);
            var output = (await outputTask.ConfigureAwait(false)).Trim();
            var error = (await errorTask.ConfigureAwait(false)).Trim();

            if (process.ExitCode == 0)
            {
                _logger.Info(string.IsNullOrWhiteSpace(output)
                    ? $"wfview server restart completed on {host}."
                    : $"wfview server restart completed on {host}: {output}");
                return;
            }

            _logger.Error($"wfview server restart failed on {host}: exit {process.ExitCode}; {error}");
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to read wfview server restart result", exception);
        }
        finally
        {
            process.Dispose();
        }
    }
}
