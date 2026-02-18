using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

/// <summary>
/// Loads and saves presets as JSON files in a presets directory next to the executable.
/// </summary>
public sealed class FilePresetRepository : IPresetRepository
{
    private static readonly JsonSerializerOptions s_readOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IFileSystem _fileSystem;
    private readonly string _presetsDirectory;

    /// <summary>Creates a repository using the real file system.</summary>
    public FilePresetRepository(string? presetsDirectory = null)
        : this(new FileSystem(), presetsDirectory)
    {
    }

    /// <summary>Creates a repository using the provided file system (e.g. MockFileSystem for tests).</summary>
    public FilePresetRepository(IFileSystem fileSystem, string? presetsDirectory = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _presetsDirectory = presetsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets");
    }

    public IReadOnlyList<PresetInfo> GetAll()
    {
        EnsurePresetsDirectoryExists();
        var list = new List<PresetInfo>();
        foreach (var path in _fileSystem.Directory.EnumerateFiles(_presetsDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            var id = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            var preset = LoadFromPath(id, path);
            if (preset != null)
            {
                var name = preset.Name?.Trim();
                list.Add(new PresetInfo(id, string.IsNullOrEmpty(name) ? id : name));
            }
        }
        return list.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public Preset? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        EnsurePresetsDirectoryExists();
        var path = Path.Combine(_presetsDirectory, id + ".json");
        return _fileSystem.File.Exists(path) ? LoadFromPath(id, path) : null;
    }

    public void Save(string id, Preset preset)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Preset id cannot be null or empty.", nameof(id));
        }
        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Preset id contains invalid filename characters.", nameof(id));
        }
        ArgumentNullException.ThrowIfNull(preset);

        EnsurePresetsDirectoryExists();
        var path = Path.Combine(_presetsDirectory, id + ".json");
        var dto = new PresetFileDto { Name = preset.Name, Config = preset.Config };
        var json = JsonSerializer.Serialize(dto, s_writeOptions);
        _fileSystem.File.WriteAllText(path, json);
    }

    public string Create(Preset preset)
    {
        EnsurePresetsDirectoryExists();
        var existing = GetAll();
        int n = 1;
        string id;
        do
        {
            id = $"preset-{n}";
            n++;
        }
        while (existing.Any(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase)));

        Save(id, preset);
        return id;
    }

    public void Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return;
        }
        var path = Path.Combine(_presetsDirectory, id + ".json");
        if (_fileSystem.File.Exists(path))
        {
            _fileSystem.File.Delete(path);
        }
    }

    private void EnsurePresetsDirectoryExists()
    {
        if (!_fileSystem.Directory.Exists(_presetsDirectory))
        {
            _fileSystem.Directory.CreateDirectory(_presetsDirectory);
        }
    }

    private Preset? LoadFromPath(string id, string path)
    {
        try
        {
            var json = _fileSystem.File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<PresetFileDto>(json, s_readOptions);
            if (dto == null)
            {
                return null;
            }
            return new Preset
            {
                Id = id,
                Name = dto.Name ?? id,
                Config = dto.Config ?? new TextLayersVisualizerSettings()
            };
        }
        catch
        {
            /* Skip invalid preset file */
            return null;
        }
    }

    private sealed class PresetFileDto
    {
        public string? Name { get; set; }
        public TextLayersVisualizerSettings? Config { get; set; }
    }
}
