using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

public sealed class FileSettingsRepository : ISettingsRepository, IVisualizerSettingsRepository
{
    private static readonly JsonSerializerOptions s_readOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IFileSystem _fileSystem;
    private readonly string _settingsPath;
    private readonly IPresetRepository _presetRepo;

    /// <summary>Creates a repository using the real file system.</summary>
    public FileSettingsRepository(IPresetRepository presetRepo, string? settingsPath = null)
        : this(new FileSystem(), presetRepo, settingsPath)
    {
    }

    /// <summary>Creates a repository using the provided file system (e.g. MockFileSystem for tests).</summary>
    public FileSettingsRepository(IFileSystem fileSystem, IPresetRepository presetRepo, string? settingsPath = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
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
        EnsureVisualizerSettingsStructure(file);
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

    /// <summary>Updates SettingsFile.VisualizerSettings for persistence. Only ActivePresetId, ApplicationMode, ActiveShowId and non-preset fields are persisted; Presets and TextLayers live in preset files.</summary>
    private static void UpdateVisualizerSettingsForSave(SettingsFile file, VisualizerSettings settings)
    {
        file.VisualizerSettings ??= new VisualizerSettings();
        file.VisualizerSettings!.ActivePresetId = settings.ActivePresetId;
        file.VisualizerSettings.ApplicationMode = settings.ApplicationMode;
        file.VisualizerSettings.ActiveShowId = settings.ActiveShowId;
    }

    private SettingsFile LoadFile()
    {
        if (!_fileSystem.File.Exists(_settingsPath))
        {
            var file = new SettingsFile();
            SaveFile(file);
            return file;
        }
        try
        {
            var json = _fileSystem.File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<SettingsFile>(json, s_readOptions) ?? new SettingsFile();
        }
        catch (Exception)
        {
            return BackupCorruptSettingsAndReset();
        }
    }

    /// <summary>Backs up corrupt or incompatible settings to {name}.{timestamp}.bak and replaces the file with defaults. Per ADR-0029.</summary>
    private SettingsFile BackupCorruptSettingsAndReset()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsPath) ?? ".";
            var baseName = Path.GetFileNameWithoutExtension(_settingsPath);
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss.fff", CultureInfo.InvariantCulture);
            var backupPath = Path.Combine(dir, $"{baseName}.{timestamp}.bak");
            _fileSystem.File.Copy(_settingsPath, backupPath, overwrite: false);
        }
        catch (Exception ex)
        {
            /* Backup failed (e.g. disk full); continue with reset so the app can run */
            System.Diagnostics.Debug.WriteLine($"Settings backup failed: {ex.Message}");
        }
        var file = new SettingsFile();
        SaveFile(file);
        return file;
    }

    private void SaveFile(SettingsFile file)
    {
        if (file.VisualizerSettings != null)
        {
            file.VisualizerSettings.Presets = null!;
            file.VisualizerSettings.TextLayers = null!;
        }
        var json = JsonSerializer.Serialize(file, s_writeOptions);
        _fileSystem.File.WriteAllText(_settingsPath, json);
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

    /// <summary>Ensures VisualizerSettings exists and TextLayers has at least 9 layers when present. Per ADR-0029, no migration of legacy formats.</summary>
    private static void EnsureVisualizerSettingsStructure(SettingsFile file)
    {
        file.VisualizerSettings ??= new VisualizerSettings();
        if (file.VisualizerSettings.TextLayers is not null)
        {
            EnsureTextLayersHasNineLayers(file.VisualizerSettings.TextLayers);
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
