using System.Text.Json;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

public sealed class FileSettingsRepository : ISettingsRepository, IVisualizerSettingsRepository
{
    private static readonly JsonSerializerOptions s_readOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions s_writeOptions = new() { WriteIndented = true };

    private readonly string _settingsPath;

    public FileSettingsRepository(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
    }

    public AppSettings LoadAppSettings()
    {
        var file = LoadFile();
        return MapToAppSettings(file);
    }

    public void SaveAppSettings(AppSettings settings)
    {
        var file = LoadFile();
        UpdateAppSettings(file, settings);
        SaveFile(file);
    }

    public VisualizerSettings LoadVisualizerSettings()
    {
        var file = LoadFile();
        MergeLegacyVisualizerSettings(file);
        return file.VisualizerSettings ?? CreateDefaultVisualizerSettings();
    }

    public void SaveVisualizerSettings(VisualizerSettings settings)
    {
        var file = LoadFile();
        file.VisualizerSettings = settings;
        SaveFile(file);
    }

    private SettingsFile LoadFile()
    {
        if (!File.Exists(_settingsPath))
        {
            var file = new SettingsFile();
            SaveFile(file);
            return file;
        }
        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<SettingsFile>(json, s_readOptions) ?? new SettingsFile();
        }
        catch
        {
            return new SettingsFile();
        }
    }

    private void SaveFile(SettingsFile file)
    {
        var json = JsonSerializer.Serialize(file, s_writeOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static AppSettings MapToAppSettings(SettingsFile file)
    {
        var mode = file.VisualizationMode ?? "textlayers";
        if (string.Equals(mode, "oscilloscope", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "unknownpleasures", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "spectrum", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "vumeter", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(mode, "winamp", StringComparison.OrdinalIgnoreCase))
        {
            mode = "textlayers";
        }
        return new AppSettings
        {
            InputMode = file.InputMode ?? "loopback",
            DeviceName = file.DeviceName,
            VisualizationMode = mode,
            BeatSensitivity = file.BeatSensitivity,
            BeatCircles = file.BeatCircles,
            OscilloscopeGain = file.OscilloscopeGain,
            SelectedPaletteId = file.SelectedPaletteId
        };
    }

    private static void UpdateAppSettings(SettingsFile file, AppSettings settings)
    {
        file.InputMode = settings.InputMode;
        file.DeviceName = settings.DeviceName;
        file.VisualizationMode = settings.VisualizationMode;
        file.BeatSensitivity = settings.BeatSensitivity;
        file.BeatCircles = settings.BeatCircles;
        file.OscilloscopeGain = settings.OscilloscopeGain;
        file.SelectedPaletteId = settings.SelectedPaletteId;
    }

    /// <summary>Merges legacy top-level BeatCircles/OscilloscopeGain into VisualizerSettings for backward compatibility. Migrates SelectedPaletteId to per-visualizer PaletteId.</summary>
    private static void MergeLegacyVisualizerSettings(SettingsFile file)
    {
        file.VisualizerSettings ??= new VisualizerSettings();

        if (file.VisualizerSettings.TextLayers is null)
        {
            file.VisualizerSettings.TextLayers = CreateDefaultTextLayersSettings();
            ApplyLegacyBeatCircles(file.VisualizerSettings.TextLayers, file.BeatCircles);
            ApplyLegacyOscilloscopeGain(file.VisualizerSettings.TextLayers, file.OscilloscopeGain);
        }
        else
        {
            EnsureTextLayersHasNineLayers(file.VisualizerSettings.TextLayers);
            ApplyLegacyBeatCircles(file.VisualizerSettings.TextLayers, file.BeatCircles);
            ApplyLegacyOscilloscopeGain(file.VisualizerSettings.TextLayers, file.OscilloscopeGain);
        }

        MigrateUnknownPleasuresToLayer(file);
        MigrateSpectrumVuMeterWinampToLayers(file);

        var globalPaletteId = file.SelectedPaletteId;
        if (!string.IsNullOrWhiteSpace(globalPaletteId) && file.VisualizerSettings.TextLayers is not null)
        {
            if (string.IsNullOrWhiteSpace(file.VisualizerSettings.TextLayers.PaletteId))
            {
                file.VisualizerSettings.TextLayers.PaletteId = globalPaletteId;
            }
        }
    }

    /// <summary>Migrates users from standalone Unknown Pleasures mode to TextLayers with an UnknownPleasures layer.</summary>
    private static void MigrateUnknownPleasuresToLayer(SettingsFile file)
    {
        if (!string.Equals(file.VisualizationMode, "unknownpleasures", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        file.VisualizationMode = "textlayers";

        var textLayers = file.VisualizerSettings?.TextLayers;
        if (textLayers is null)
        {
            return;
        }

        var legacyPaletteId = file.VisualizerSettings?.UnknownPleasures?.PaletteId;
        if (string.IsNullOrWhiteSpace(legacyPaletteId))
        {
            return;
        }

        textLayers.Layers ??= new List<TextLayerSettings>();
        int maxZ = textLayers.Layers.Count > 0 ? textLayers.Layers.Max(l => l.ZOrder) : -1;
        textLayers.Layers.Insert(0, new TextLayerSettings
        {
            LayerType = TextLayerType.UnknownPleasures,
            ZOrder = maxZ + 1,
            Enabled = true,
            PaletteId = legacyPaletteId,
            BeatReaction = TextLayerBeatReaction.None,
            SpeedMultiplier = 1.0
        });
    }

    /// <summary>Migrates users from standalone spectrum/vumeter/winamp mode to TextLayers with the corresponding layer.</summary>
    private static void MigrateSpectrumVuMeterWinampToLayers(SettingsFile file)
    {
        var legacyMode = file.VisualizationMode ?? "";
        if (!string.Equals(legacyMode, "spectrum", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(legacyMode, "vumeter", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(legacyMode, "winamp", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        file.VisualizationMode = "textlayers";

        var textLayers = file.VisualizerSettings?.TextLayers;
        if (textLayers is null)
        {
            return;
        }

        textLayers.Layers ??= new List<TextLayerSettings>();
        int maxZ = textLayers.Layers.Count > 0 ? textLayers.Layers.Max(l => l.ZOrder) : -1;

        if (string.Equals(legacyMode, "vumeter", StringComparison.OrdinalIgnoreCase))
        {
            textLayers.Layers.Add(new TextLayerSettings
            {
                LayerType = TextLayerType.VuMeter,
                ZOrder = maxZ + 1,
                Enabled = true,
                BeatReaction = TextLayerBeatReaction.None,
                SpeedMultiplier = 1.0
            });
        }
        else if (string.Equals(legacyMode, "spectrum", StringComparison.OrdinalIgnoreCase))
        {
            textLayers.Layers.Add(new TextLayerSettings
            {
                LayerType = TextLayerType.LlamaStyle,
                ZOrder = maxZ + 1,
                Enabled = true,
                LlamaStyleShowVolumeBar = true,
                LlamaStyleShowRowLabels = true,
                LlamaStyleShowFrequencyLabels = true,
                LlamaStyleColorScheme = "Spectrum",
                LlamaStylePeakMarkerStyle = "DoubleLine",
                LlamaStyleBarWidth = 2,
                BeatReaction = TextLayerBeatReaction.None,
                SpeedMultiplier = 1.0
            });
        }
        else if (string.Equals(legacyMode, "winamp", StringComparison.OrdinalIgnoreCase))
        {
            textLayers.Layers.Add(new TextLayerSettings
            {
                LayerType = TextLayerType.LlamaStyle,
                ZOrder = maxZ + 1,
                Enabled = true,
                BeatReaction = TextLayerBeatReaction.None,
                SpeedMultiplier = 1.0
            });
        }
    }

    private static VisualizerSettings CreateDefaultVisualizerSettings()
    {
        var s = new VisualizerSettings
        {
            TextLayers = CreateDefaultTextLayersSettings()
        };
        return s;
    }

    /// <summary>Default: 9 layers with GeissBackground + BeatCircles, then varied foreground types. Keys 1–9 map to layers 1–9.</summary>
    private static TextLayersVisualizerSettings CreateDefaultTextLayersSettings()
    {
        var layers = new List<TextLayerSettings>
        {
            new() { LayerType = TextLayerType.GeissBackground, ZOrder = 0, BeatReaction = TextLayerBeatReaction.Flash, SpeedMultiplier = 1.0 },
            new() { LayerType = TextLayerType.BeatCircles, ZOrder = 1, BeatReaction = TextLayerBeatReaction.None, SpeedMultiplier = 1.0 },
            new() { LayerType = TextLayerType.Marquee, ZOrder = 2, TextSnippets = ["Layered text", "Audio visualizer"], BeatReaction = TextLayerBeatReaction.SpeedBurst, SpeedMultiplier = 1.0 },
            new() { LayerType = TextLayerType.Marquee, ZOrder = 3, TextSnippets = ["Layer 3"], BeatReaction = TextLayerBeatReaction.None, SpeedMultiplier = 0.8 },
            new() { LayerType = TextLayerType.WaveText, ZOrder = 4, TextSnippets = ["Wave"], BeatReaction = TextLayerBeatReaction.Pulse, SpeedMultiplier = 1.0 },
            new() { LayerType = TextLayerType.StaticText, ZOrder = 5, TextSnippets = ["Static"], BeatReaction = TextLayerBeatReaction.None },
            new() { LayerType = TextLayerType.FallingLetters, ZOrder = 6, TextSnippets = [".*#%"], BeatReaction = TextLayerBeatReaction.SpawnMore, SpeedMultiplier = 1.0 },
            new() { LayerType = TextLayerType.MatrixRain, ZOrder = 7, BeatReaction = TextLayerBeatReaction.Flash, SpeedMultiplier = 1.0 },
            new() { LayerType = TextLayerType.StaticText, ZOrder = 8, TextSnippets = ["Top"], BeatReaction = TextLayerBeatReaction.Pulse }
        };
        return new TextLayersVisualizerSettings { Layers = layers };
    }

    /// <summary>Applies legacy OscilloscopeGain to the first Oscilloscope layer in TextLayers. If none exists and legacy gain differs from default, adds an Oscilloscope layer.</summary>
    private static void ApplyLegacyOscilloscopeGain(TextLayersVisualizerSettings textLayers, double legacyGain)
    {
        textLayers.Layers ??= new List<TextLayerSettings>();
        var firstOsc = textLayers.Layers.FirstOrDefault(l => l.LayerType == TextLayerType.Oscilloscope);
        if (firstOsc != null)
        {
            firstOsc.Gain = Math.Clamp(legacyGain, 1.0, 10.0);
        }
        else if (Math.Abs(legacyGain - 2.5) > 0.01)
        {
            int maxZ = textLayers.Layers.Count > 0 ? textLayers.Layers.Max(l => l.ZOrder) : -1;
            textLayers.Layers.Add(new TextLayerSettings
            {
                LayerType = TextLayerType.Oscilloscope,
                ZOrder = maxZ + 1,
                Gain = Math.Clamp(legacyGain, 1.0, 10.0),
                BeatReaction = TextLayerBeatReaction.None,
                SpeedMultiplier = 1.0
            });
        }
    }

    /// <summary>Applies legacy BeatCircles setting to TextLayers. When file.BeatCircles is false, disables BeatCircles layers.</summary>
    private static void ApplyLegacyBeatCircles(TextLayersVisualizerSettings textLayers, bool legacyBeatCircles)
    {
        if (legacyBeatCircles)
        {
            return;
        }
        if (textLayers.Layers is null)
        {
            return;
        }
        foreach (var layer in textLayers.Layers)
        {
            if (layer.LayerType == TextLayerType.BeatCircles)
            {
                layer.Enabled = false;
            }
        }
    }

    /// <summary>Ensures TextLayers has at least 9 layers so keys 1–9 always map to a layer. Pads with default layers if fewer.</summary>
    private static void EnsureTextLayersHasNineLayers(TextLayersVisualizerSettings textLayers)
    {
        textLayers.Layers ??= new List<TextLayerSettings>();
        int maxZ = textLayers.Layers.Count > 0 ? textLayers.Layers.Max(l => l.ZOrder) : -1;
        while (textLayers.Layers.Count < 9)
        {
            maxZ++;
            textLayers.Layers.Add(new TextLayerSettings
            {
                LayerType = TextLayerType.Marquee,
                ZOrder = maxZ,
                TextSnippets = [$"Layer {textLayers.Layers.Count + 1}"],
                BeatReaction = TextLayerBeatReaction.None,
                SpeedMultiplier = 1.0
            });
        }
    }

    /// <summary>Internal DTO for JSON serialization. Holds both app-level and visualizer settings.</summary>
    private sealed class SettingsFile
    {
        public string InputMode { get; set; } = "loopback";
        public string? DeviceName { get; set; }
        public string VisualizationMode { get; set; } = "textlayers";
        public double BeatSensitivity { get; set; } = 1.3;
        public bool BeatCircles { get; set; } = true;
        public double OscilloscopeGain { get; set; } = 2.5;
        public string? SelectedPaletteId { get; set; }
        public VisualizerSettings? VisualizerSettings { get; set; }
    }
}
