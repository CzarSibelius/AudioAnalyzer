using System.Text.Json;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

public sealed class FileSettingsRepository : ISettingsRepository
{
    private static readonly JsonSerializerOptions s_readOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions s_writeOptions = new() { WriteIndented = true };

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
            var settings = JsonSerializer.Deserialize<AppSettings>(json, s_readOptions) ?? new AppSettings();
            MergeLegacyVisualizerSettings(settings);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>Merges legacy top-level BeatCircles/OscilloscopeGain into VisualizerSettings for backward compatibility.</summary>
    private static void MergeLegacyVisualizerSettings(AppSettings settings)
    {
        settings.VisualizerSettings ??= new VisualizerSettings();
        if (settings.VisualizerSettings.Geiss is null)
        {
            settings.VisualizerSettings.Geiss = new GeissVisualizerSettings { BeatCircles = settings.BeatCircles };
        }

        if (settings.VisualizerSettings.Oscilloscope is null)
        {
            settings.VisualizerSettings.Oscilloscope = new OscilloscopeVisualizerSettings { Gain = settings.OscilloscopeGain };
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, s_writeOptions);
        File.WriteAllText(_settingsPath, json);
    }
}
