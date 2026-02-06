using System.Text.Json;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

public sealed class FileSettingsRepository : ISettingsRepository
{
    private readonly string _settingsPath;

    public FileSettingsRepository(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            var defaults = new AppSettings();
            Save(defaults);
            return defaults;
        }
        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }
}
