using System.IO;
using System.Text.Json;
using W3NTBPowerBridge.Models;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Stores settings as JSON in the user's AppData folder.
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;

    /// <summary>
    /// Creates the JSON settings service.
    /// </summary>
    public JsonSettingsService()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "W3NTBPowerBridge");
        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "settings.json");
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = new AppSettings();
            await SaveAsync(defaults).ConfigureAwait(false);
            return defaults;
        }

        await using var stream = File.OpenRead(_settingsPath);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream).ConfigureAwait(false) ?? new AppSettings();
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings)
    {
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions).ConfigureAwait(false);
    }
}
