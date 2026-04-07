using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

/// <summary>Persists app and visualizer settings in <c>appsettings.json</c>; preset bodies live in preset files. Uses <see cref="IDefaultTextLayersSettingsFactory"/> for typed default TextLayers so this assembly does not reference Visualizers.</summary>
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
    private readonly IDefaultTextLayersSettingsFactory _defaultTextLayersFactory;

    /// <summary>Creates a repository using the real file system.</summary>
    public FileSettingsRepository(IPresetRepository presetRepo, IDefaultTextLayersSettingsFactory defaultTextLayersFactory, string? settingsPath = null)
        : this(new FileSystem(), presetRepo, defaultTextLayersFactory, settingsPath)
    {
    }

    /// <summary>Creates a repository using the provided file system (e.g. MockFileSystem for tests).</summary>
    public FileSettingsRepository(IFileSystem fileSystem, IPresetRepository presetRepo, IDefaultTextLayersSettingsFactory defaultTextLayersFactory, string? settingsPath = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _presetRepo = presetRepo ?? throw new ArgumentNullException(nameof(presetRepo));
        _defaultTextLayersFactory = defaultTextLayersFactory ?? throw new ArgumentNullException(nameof(defaultTextLayersFactory));
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
            var created = _presetRepo.Create(new Preset { Name = "Preset 1", Config = vs.TextLayers?.DeepCopy() ?? _defaultTextLayersFactory.CreateDefault() });
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
            TextLayersPersistenceNormalization.NormalizeLayerList(vs.TextLayers);
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
            BpmSource = file.BpmSource,
            BeatSensitivity = file.BeatSensitivity,
            BeatCircles = file.BeatCircles,
            OscilloscopeGain = file.OscilloscopeGain,
            SelectedPaletteId = file.SelectedPaletteId,
            UiSettings = MapToUiSettings(file.UiSettings)
        };
    }

    private static void UpdateAppSettings(SettingsFile file, AppSettings settings)
    {
        file.InputMode = settings.InputMode;
        file.DeviceName = settings.DeviceName;
        file.VisualizationMode = "textlayers";
        file.BpmSource = settings.BpmSource;
        file.BeatSensitivity = settings.BeatSensitivity;
        file.BeatCircles = settings.BeatCircles;
        file.OscilloscopeGain = settings.OscilloscopeGain;
        file.SelectedPaletteId = settings.SelectedPaletteId;
        file.UiSettings = MapToUiSettingsFile(settings.UiSettings);
    }

    private static UiSettings MapToUiSettings(UiSettingsFile? file)
    {
        if (file == null)
        {
            return new UiSettings();
        }

        var palette = new UiPalette
        {
            Normal = ColorPaletteParser.ParseEntry(file.Palette?.Normal),
            Highlighted = ColorPaletteParser.ParseEntry(file.Palette?.Highlighted),
            Dimmed = ColorPaletteParser.ParseEntry(file.Palette?.Dimmed),
            Label = ColorPaletteParser.ParseEntry(file.Palette?.Label),
            Background = file.Palette?.Background != null ? ColorPaletteParser.ParseEntry(file.Palette.Background) : null
        };

        return new UiSettings
        {
            Title = file.Title ?? "AUDIO ANALYZER - Real-time Frequency Spectrum",
            DefaultScrollingSpeed = file.DefaultScrollingSpeed,
            Palette = palette,
            UiThemeId = string.IsNullOrWhiteSpace(file.UiThemeId) ? null : file.UiThemeId.Trim(),
            DefaultAssetFolderPath = string.IsNullOrWhiteSpace(file.DefaultAssetFolderPath) ? null : file.DefaultAssetFolderPath.Trim(),
            ShowRenderFps = file.ShowRenderFps
        };
    }

    private static UiSettingsFile MapToUiSettingsFile(UiSettings? settings)
    {
        if (settings == null)
        {
            return new UiSettingsFile();
        }

        var palette = settings.Palette ?? new UiPalette();
        return new UiSettingsFile
        {
            Title = settings.Title,
            DefaultScrollingSpeed = settings.DefaultScrollingSpeed,
            UiThemeId = string.IsNullOrWhiteSpace(settings.UiThemeId) ? null : settings.UiThemeId.Trim(),
            DefaultAssetFolderPath = string.IsNullOrWhiteSpace(settings.DefaultAssetFolderPath) ? null : settings.DefaultAssetFolderPath.Trim(),
            ShowRenderFps = settings.ShowRenderFps,
            Palette = new UiPaletteFile
            {
                Normal = ColorPaletteParser.ToEntry(palette.Normal),
                Highlighted = ColorPaletteParser.ToEntry(palette.Highlighted),
                Dimmed = ColorPaletteParser.ToEntry(palette.Dimmed),
                Label = ColorPaletteParser.ToEntry(palette.Label),
                Background = palette.Background.HasValue ? ColorPaletteParser.ToEntry(palette.Background.Value) : null
            }
        };
    }

    /// <summary>Ensures VisualizerSettings exists and normalizes embedded TextLayers layer list when present (cap only; per ADR-0070 no padding).</summary>
    private static void EnsureVisualizerSettingsStructure(SettingsFile file)
    {
        file.VisualizerSettings ??= new VisualizerSettings();
        if (file.VisualizerSettings.TextLayers is not null)
        {
            TextLayersPersistenceNormalization.NormalizeLayerList(file.VisualizerSettings.TextLayers);
        }
    }

    private VisualizerSettings CreateDefaultVisualizerSettings()
    {
        var defaultConfig = _defaultTextLayersFactory.CreateDefault();
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

    /// <summary>Internal DTO for JSON serialization. Holds both app-level and visualizer settings.</summary>
    private sealed class SettingsFile
    {
        public string InputMode { get; set; } = "loopback";
        public string? DeviceName { get; set; }
        public string VisualizationMode { get; set; } = "textlayers";
        public BpmSource BpmSource { get; set; } = BpmSource.AudioAnalysis;
        public double BeatSensitivity { get; set; } = 1.3;
        public bool BeatCircles { get; set; } = true;
        public double OscilloscopeGain { get; set; } = 2.5;
        public string? SelectedPaletteId { get; set; }
        public VisualizerSettings? VisualizerSettings { get; set; }
        public UiSettingsFile? UiSettings { get; set; }
    }

    /// <summary>JSON DTO for UiSettings persistence.</summary>
    private sealed class UiSettingsFile
    {
        public string? Title { get; set; }
        public double DefaultScrollingSpeed { get; set; } = 0.25;
        public string? UiThemeId { get; set; }
        public string? DefaultAssetFolderPath { get; set; }
        public bool ShowRenderFps { get; set; }
        public UiPaletteFile? Palette { get; set; }
    }

    /// <summary>JSON DTO for UiPalette persistence. Uses PaletteColorEntry for each slot.</summary>
    private sealed class UiPaletteFile
    {
        public PaletteColorEntry? Normal { get; set; }
        public PaletteColorEntry? Highlighted { get; set; }
        public PaletteColorEntry? Dimmed { get; set; }
        public PaletteColorEntry? Label { get; set; }
        public PaletteColorEntry? Background { get; set; }
    }
}
