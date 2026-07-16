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
    /// Launches ACLog using the configured path.
    /// </summary>
    /// <param name="settings">Current settings.</param>
    void LaunchAcLog(AppSettings settings);
}
