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

    /// <summary>Merges legacy top-level BeatCircles/OscilloscopeGain into VisualizerSettings for backward compatibility. Migrates SelectedPaletteId to per-visualizer PaletteId.</summary>
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

        if (settings.VisualizerSettings.TextLayers is null)
        {
            settings.VisualizerSettings.TextLayers = CreateDefaultTextLayersSettings();
        }

        if (settings.VisualizerSettings.UnknownPleasures is null)
        {
            settings.VisualizerSettings.UnknownPleasures = new UnknownPleasuresVisualizerSettings();
        }

        // Migrate global SelectedPaletteId to per-visualizer PaletteId for backward compatibility
        var globalPaletteId = settings.SelectedPaletteId;
        if (!string.IsNullOrWhiteSpace(globalPaletteId))
        {
            if (string.IsNullOrWhiteSpace(settings.VisualizerSettings.Geiss.PaletteId))
            {
                settings.VisualizerSettings.Geiss.PaletteId = globalPaletteId;
            }
            if (string.IsNullOrWhiteSpace(settings.VisualizerSettings.UnknownPleasures.PaletteId))
            {
                settings.VisualizerSettings.UnknownPleasures.PaletteId = globalPaletteId;
            }
            if (string.IsNullOrWhiteSpace(settings.VisualizerSettings.TextLayers.PaletteId))
            {
                settings.VisualizerSettings.TextLayers.PaletteId = globalPaletteId;
            }
        }
    }

    /// <summary>Default layers: ScrollingColors background + Marquee foreground with a snippet.</summary>
    private static TextLayersVisualizerSettings CreateDefaultTextLayersSettings()
    {
        return new TextLayersVisualizerSettings
        {
            Layers =
            [
                new TextLayerSettings
                {
                    LayerType = TextLayerType.ScrollingColors,
                    ZOrder = 0,
                    BeatReaction = TextLayerBeatReaction.ColorPop,
                    SpeedMultiplier = 1.0
                },
                new TextLayerSettings
                {
                    LayerType = TextLayerType.Marquee,
                    ZOrder = 1,
                    TextSnippets = ["Layered text", "Audio visualizer"],
                    BeatReaction = TextLayerBeatReaction.SpeedBurst,
                    SpeedMultiplier = 1.0
                }
            ]
        };
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, s_writeOptions);
        File.WriteAllText(_settingsPath, json);
    }
}
