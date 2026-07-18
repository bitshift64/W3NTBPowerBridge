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
    private const int ServerStopTimeoutSeconds = 30;
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
    public void CloseWfview()
    {
        CloseProcesses("wfview", "wfview");
    }

    /// <inheritdoc />
    public void LaunchAcLog(AppSettings settings)
    {
        Launch(settings.AcLogPath, "ACLog");
    }

    /// <inheritdoc />
    public void CloseAcLog()
    {
        CloseProcesses("ACLog", "ACLog");
    }

    /// <inheritdoc />
    public Task<bool> LaunchWfviewServerAsync(AppSettings settings)
    {
        return LaunchWfviewServerOverSshAsync(settings.WfviewServerHost, settings.WfviewServerPath, settings.WfviewServerAudioWaitSeconds);
    }

    /// <inheritdoc />
    public Task<bool> StopWfviewServerAsync(AppSettings settings)
    {
        return StopWfviewServerOverSshAsync(settings.WfviewServerHost);
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

    private void CloseProcesses(string processName, string displayName)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                _logger.Info($"{displayName} is not running.");
                return;
            }

            foreach (var process in processes)
            {
                using (process)
                {
                    if (!process.CloseMainWindow())
                    {
                        process.Kill(entireProcessTree: true);
                    }
                    else if (!process.WaitForExit(5000))
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
            }

            _logger.Info($"Closed {displayName}.");
        }
        catch (Exception exception)
        {
            _logger.Error($"Failed to close {displayName}", exception);
        }
    }

    private async Task<bool> LaunchWfviewServerOverSshAsync(string host, string wfviewPath, int audioWaitSeconds)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.Info("wfview server host is not configured.");
            return false;
        }

        var path = string.IsNullOrWhiteSpace(wfviewPath) ? @"C:\Program Files\wfview\wfview.exe" : wfviewPath;
        var escapedPath = path.Replace("'", "''");
        var safeAudioWaitSeconds = Math.Clamp(audioWaitSeconds, 0, 120);
        var script = $$"""
            $taskName = 'W3NTB Launch wfview'
            $wfviewPath = '{{escapedPath}}'
            $userId = (whoami).Trim()
            $audioReady = $false
            for ($attempt = 0; $attempt -lt {{safeAudioWaitSeconds}}; $attempt++) {
                $audioDevices = @(Get-PnpDevice -Class AudioEndpoint -ErrorAction SilentlyContinue | Where-Object { $_.Status -eq 'OK' -and $_.FriendlyName -like '*USB Audio CODEC*' })
                $hasInput = $audioDevices | Where-Object { $_.FriendlyName -like 'Microphone*' }
                $hasOutput = $audioDevices | Where-Object { $_.FriendlyName -like 'Speakers*' }
                if ($hasInput -and $hasOutput) {
                    $audioReady = $true
                    break
                }

                Start-Sleep -Seconds 1
            }

            if ($audioReady) {
                'USB Audio CODEC devices are ready.'
                Start-Sleep -Seconds 5
            } else {
                'USB Audio CODEC devices were not fully ready before launch.'
            }

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
            var process = Process.Start(new ProcessStartInfo("ssh.exe", $"-o ConnectTimeout=10 -o BatchMode=yes {host} powershell.exe -NoProfile -EncodedCommand {encodedCommand}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process is null)
            {
                _logger.Error("Failed to start SSH process for wfview server restart.");
                return false;
            }

            _logger.Info($"Requested wfview server restart on {host}.");
            return await LogServerLaunchResultAsync(process, host, safeAudioWaitSeconds + 30).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to launch wfview server restart command", exception);
            return false;
        }
    }

    private async Task<bool> StopWfviewServerOverSshAsync(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            _logger.Info("wfview server host is not configured.");
            return false;
        }

        const string script = """
            Stop-Process -Name wfview -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
            $remaining = Get-Process -Name wfview -ErrorAction SilentlyContinue
            if ($remaining) {
                'wfview still running'
                exit 1
            }

            'wfview stopped'
            """;
        var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

        try
        {
            var process = Process.Start(new ProcessStartInfo("ssh.exe", $"-o ConnectTimeout=10 -o BatchMode=yes {host} powershell.exe -NoProfile -EncodedCommand {encodedCommand}")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            if (process is null)
            {
                _logger.Error("Failed to start SSH process for wfview server stop.");
                return false;
            }

            _logger.Info($"Requested wfview server stop on {host}.");
            return await LogServerStopResultAsync(process, host, ServerStopTimeoutSeconds).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to launch wfview server stop command", exception);
            return false;
        }
    }

    private async Task<bool> LogServerLaunchResultAsync(Process process, string host, int timeoutSeconds)
    {
        try
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!await WaitForExitOrKillAsync(process, timeoutSeconds).ConfigureAwait(false))
            {
                _logger.Error($"wfview server restart timed out on {host} after {timeoutSeconds} seconds.");
                return false;
            }

            var output = (await outputTask.ConfigureAwait(false)).Trim();
            var error = (await errorTask.ConfigureAwait(false)).Trim();

            if (process.ExitCode == 0)
            {
                _logger.Info(string.IsNullOrWhiteSpace(output)
                    ? $"wfview server restart completed on {host}."
                    : $"wfview server restart completed on {host}: {output}");
                return true;
            }

            _logger.Error($"wfview server restart failed on {host}: exit {process.ExitCode}; {error}");
            return false;
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to read wfview server restart result", exception);
            return false;
        }
        finally
        {
            process.Dispose();
        }
    }

    private async Task<bool> LogServerStopResultAsync(Process process, string host, int timeoutSeconds)
    {
        try
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            if (!await WaitForExitOrKillAsync(process, timeoutSeconds).ConfigureAwait(false))
            {
                _logger.Error($"wfview server stop timed out on {host} after {timeoutSeconds} seconds.");
                return false;
            }

            var output = (await outputTask.ConfigureAwait(false)).Trim();
            var error = (await errorTask.ConfigureAwait(false)).Trim();

            if (process.ExitCode == 0)
            {
                _logger.Info(string.IsNullOrWhiteSpace(output)
                    ? $"wfview server stop completed on {host}."
                    : $"wfview server stop completed on {host}: {output}");
                return true;
            }

            _logger.Error($"wfview server stop failed on {host}: exit {process.ExitCode}; {error}");
            return false;
        }
        catch (Exception exception)
        {
            _logger.Error("Failed to read wfview server stop result", exception);
            return false;
        }
        finally
        {
            process.Dispose();
        }
    }

    private static async Task<bool> WaitForExitOrKillAsync(Process process, int timeoutSeconds)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 5, 180)));
        try
        {
            await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Best-effort cleanup after a stuck SSH command.
            }

            return false;
        }
    }
}
