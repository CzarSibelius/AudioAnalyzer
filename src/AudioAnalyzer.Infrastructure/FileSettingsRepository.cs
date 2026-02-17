using System.Text.Json;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

public sealed class FileSettingsRepository : ISettingsRepository, IVisualizerSettingsRepository
{
    private static readonly JsonSerializerOptions s_readOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _settingsPath;
    private readonly IPresetRepository _presetRepo;

    public FileSettingsRepository(IPresetRepository presetRepo, string? settingsPath = null)
    {
        _presetRepo = presetRepo ?? throw new ArgumentNullException(nameof(presetRepo));
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
        var vs = file.VisualizerSettings ?? CreateDefaultVisualizerSettings();
        ApplyPresetsFromFiles(vs);
        return vs;
    }

    public void SaveVisualizerSettings(VisualizerSettings settings)
    {
        SyncActivePresetToFile(settings);
        var file = LoadFile();
        UpdateVisualizerSettingsForSave(file, settings);
        file.VisualizationMode = "textlayers";
        SaveFile(file);
    }

    /// <summary> syncs TextLayers into the active preset file on disk.</summary>
    private void SyncActivePresetToFile(VisualizerSettings vs)
    {
        if (vs.TextLayers is null || string.IsNullOrWhiteSpace(vs.ActivePresetId))
        {
            return;
        }
        var preset = new Preset
        {
            Id = vs.ActivePresetId,
            Name = vs.Presets?.FirstOrDefault(p => string.Equals(p.Id, vs.ActivePresetId, StringComparison.OrdinalIgnoreCase))?.Name ?? vs.ActivePresetId,
            Config = vs.TextLayers.DeepCopy()
        };
        _presetRepo.Save(vs.ActivePresetId, preset);
    }

    /// <summary>Populates Presets from preset files and syncs TextLayers from active preset.</summary>
    private void ApplyPresetsFromFiles(VisualizerSettings vs)
    {
        var all = _presetRepo.GetAll();
        vs.Presets = all.Select(p => new Preset { Id = p.Id, Name = p.Name, Config = new TextLayersVisualizerSettings() }).ToList();
        if (vs.Presets.Count == 0)
        {
            var created = _presetRepo.Create(new Preset { Name = "Preset 1", Config = vs.TextLayers?.DeepCopy() ?? CreateDefaultTextLayersSettings() });
            vs.ActivePresetId = created;
            vs.Presets = _presetRepo.GetAll().Select(p => new Preset { Id = p.Id, Name = p.Name, Config = new TextLayersVisualizerSettings() }).ToList();
        }
        var activeId = vs.ActivePresetId;
        var active = _presetRepo.GetById(activeId ?? "");
        if (active == null && vs.Presets.Count > 0)
        {
            activeId = vs.Presets[0].Id;
            active = _presetRepo.GetById(activeId);
        }
        if (active != null)
        {
            vs.ActivePresetId = activeId ?? active.Id;
            vs.TextLayers ??= new TextLayersVisualizerSettings();
            vs.TextLayers.CopyFrom(active.Config);
        }
    }

    /// <summary>Updates SettingsFile.VisualizerSettings for persistence. Only ActivePresetId and non-preset fields are persisted; Presets and TextLayers live in preset files.</summary>
    private static void UpdateVisualizerSettingsForSave(SettingsFile file, VisualizerSettings settings)
    {
        file.VisualizerSettings ??= new VisualizerSettings();
        file.VisualizerSettings!.ActivePresetId = settings.ActivePresetId;
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
        if (file.VisualizerSettings != null)
        {
            file.VisualizerSettings.Presets = null!;
            file.VisualizerSettings.TextLayers = null!;
        }
        var json = JsonSerializer.Serialize(file, s_writeOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static AppSettings MapToAppSettings(SettingsFile file)
    {
        return new AppSettings
        {
            InputMode = file.InputMode ?? "loopback",
            DeviceName = file.DeviceName,
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
        file.VisualizationMode = "textlayers";
        file.BeatSensitivity = settings.BeatSensitivity;
        file.BeatCircles = settings.BeatCircles;
        file.OscilloscopeGain = settings.OscilloscopeGain;
        file.SelectedPaletteId = settings.SelectedPaletteId;
    }

    /// <summary>Merges legacy top-level BeatCircles/OscilloscopeGain into VisualizerSettings for backward compatibility. Migrates SelectedPaletteId to per-visualizer PaletteId. Migrates TextLayers to Presets when Presets is empty. Migrates legacy Presets to preset files.</summary>
    private void MergeLegacyVisualizerSettings(SettingsFile file)
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
        MigrateTextLayerLegacyPropsToCustom(file);
        MigrateLegacyPresetsToFiles(file);
        MigrateToPresets(file);

        var globalPaletteId = file.SelectedPaletteId;
        if (!string.IsNullOrWhiteSpace(globalPaletteId) && file.VisualizerSettings.TextLayers is not null)
        {
            if (string.IsNullOrWhiteSpace(file.VisualizerSettings.TextLayers.PaletteId))
            {
                file.VisualizerSettings.TextLayers.PaletteId = globalPaletteId;
            }
        }

        SyncTextLayersFromActivePreset(file.VisualizerSettings);
    }

    /// <summary>Migrates legacy inline Presets to preset files. Clears Presets from in-memory after migration.</summary>
    private void MigrateLegacyPresetsToFiles(SettingsFile file)
    {
        var vs = file.VisualizerSettings;
        if (vs?.Presets is not { Count: > 0 })
        {
            return;
        }

        var activeId = vs.ActivePresetId;
        foreach (var preset in vs.Presets)
        {
            var id = SanitizePresetId(preset.Id);
            if (string.IsNullOrEmpty(id))
            {
                id = _presetRepo.Create(preset);
            }
            else
            {
                _presetRepo.Save(id, preset);
            }
            if (string.Equals(preset.Id, activeId, StringComparison.OrdinalIgnoreCase))
            {
                vs.ActivePresetId = id;
            }
        }
        vs.Presets.Clear();
    }

    private static string SanitizePresetId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return "";
        }
        var invalid = Path.GetInvalidFileNameChars();
        if (id.IndexOfAny(invalid) < 0)
        {
            return id;
        }
        return string.Concat(id.Select(c => Array.IndexOf(invalid, c) >= 0 ? '-' : c));
    }

    /// <summary>If Presets is empty and no preset files exist, creates one preset file from current TextLayers ("Preset 1").</summary>
    private void MigrateToPresets(SettingsFile file)
    {
        var vs = file.VisualizerSettings;
        if (vs is null)
        {
            return;
        }

        var all = _presetRepo.GetAll();
        if (all.Count > 0)
        {
            return;
        }

        var preset = new Preset
        {
            Name = "Preset 1",
            Config = (vs.TextLayers ?? CreateDefaultTextLayersSettings()).DeepCopy()
        };
        var createdId = _presetRepo.Create(preset);
        vs.ActivePresetId = createdId;
    }

    /// <summary>Migrates legacy top-level layer properties (Gain, LlamaStyle*, etc.) from ExtensionData into Custom on all layers.</summary>
    private static void MigrateTextLayerLegacyPropsToCustom(SettingsFile file)
    {
        var vs = file.VisualizerSettings;
        if (vs is null) { return; }

        foreach (var layer in vs.TextLayers?.Layers ?? [])
        {
            layer.MigrateExtensionDataToCustom();
        }
        foreach (var preset in vs.Presets ?? [])
        {
            foreach (var layer in preset.Config?.Layers ?? [])
            {
                layer.MigrateExtensionDataToCustom();
            }
        }
    }

    /// <summary>Ensures TextLayers is populated from the active preset. When loading, the live buffer gets the preset's saved config.</summary>
    private static void SyncTextLayersFromActivePreset(VisualizerSettings vs)
    {
        if (vs.Presets is not { Count: > 0 })
        {
            return;
        }

        var active = vs.Presets.FirstOrDefault(p => string.Equals(p.Id, vs.ActivePresetId, StringComparison.OrdinalIgnoreCase))
            ?? vs.Presets[0];
        vs.ActivePresetId = active.Id;
        vs.TextLayers ??= new TextLayersVisualizerSettings();
        vs.TextLayers.CopyFrom(active.Config);
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

        var paletteId = textLayers.PaletteId ?? "";

        textLayers.Layers ??= new List<TextLayerSettings>();
        int maxZ = textLayers.Layers.Count > 0 ? textLayers.Layers.Max(l => l.ZOrder) : -1;
        textLayers.Layers.Insert(0, new TextLayerSettings
        {
            LayerType = TextLayerType.UnknownPleasures,
            ZOrder = maxZ + 1,
            Enabled = true,
            PaletteId = paletteId,
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
            var layer = new TextLayerSettings
            {
                LayerType = TextLayerType.LlamaStyle,
                ZOrder = maxZ + 1,
                Enabled = true,
                BeatReaction = TextLayerBeatReaction.None,
                SpeedMultiplier = 1.0
            };
            layer.SetCustom(new { ShowVolumeBar = true, ShowRowLabels = true, ShowFrequencyLabels = true, ColorScheme = "Spectrum", PeakMarkerStyle = "DoubleLine", BarWidth = 2 });
            textLayers.Layers.Add(layer);
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
        var defaultConfig = CreateDefaultTextLayersSettings();
        var preset = new Preset
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Preset 1",
            Config = defaultConfig.DeepCopy()
        };
        var s = new VisualizerSettings
        {
            Presets = new List<Preset> { preset },
            ActivePresetId = preset.Id,
            TextLayers = defaultConfig
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
            firstOsc.SetCustom(new { Gain = Math.Clamp(legacyGain, 1.0, 10.0) });
        }
        else if (Math.Abs(legacyGain - 2.5) > 0.01)
        {
            int maxZ = textLayers.Layers.Count > 0 ? textLayers.Layers.Max(l => l.ZOrder) : -1;
            var layer = new TextLayerSettings
            {
                LayerType = TextLayerType.Oscilloscope,
                ZOrder = maxZ + 1,
                BeatReaction = TextLayerBeatReaction.None,
                SpeedMultiplier = 1.0
            };
            layer.SetCustom(new { Gain = Math.Clamp(legacyGain, 1.0, 10.0) });
            textLayers.Layers.Add(layer);
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
