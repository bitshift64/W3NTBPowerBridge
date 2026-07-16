using W3NTBPowerBridge.Models;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Loads and saves application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Loads settings from local storage, creating defaults when needed.
    /// </summary>
    /// <returns>The loaded settings.</returns>
    Task<AppSettings> LoadAsync();

    /// <summary>
    /// Saves settings to local storage.
    /// </summary>
    /// <param name="settings">Settings to save.</param>
    /// <returns>A task representing the save operation.</returns>
    Task SaveAsync(AppSettings settings);
}
