using W3NTBPowerBridge.Models;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Launches companion station applications.
/// </summary>
public interface IProcessLauncher
{
    /// <summary>
    /// Launches wfview using the configured path.
    /// </summary>
    /// <param name="settings">Current settings.</param>
    void LaunchWfview(AppSettings settings);

    /// <summary>
    /// Closes local wfview processes for the current user.
    /// </summary>
    void CloseWfview();

    /// <summary>
    /// Launches ACLog using the configured path.
    /// </summary>
    /// <param name="settings">Current settings.</param>
    void LaunchAcLog(AppSettings settings);

    /// <summary>
    /// Closes local ACLog processes for the current user.
    /// </summary>
    void CloseAcLog();

    /// <summary>
    /// Runs the configured command that starts or restarts the shack-side wfview server.
    /// </summary>
    /// <param name="settings">Current settings.</param>
    /// <returns>True when the remote launch command completed successfully.</returns>
    Task<bool> LaunchWfviewServerAsync(AppSettings settings);

    /// <summary>
    /// Stops the shack-side wfview server process.
    /// </summary>
    /// <param name="settings">Current settings.</param>
    /// <returns>True when the remote stop command completed successfully.</returns>
    Task<bool> StopWfviewServerAsync(AppSettings settings);
}
