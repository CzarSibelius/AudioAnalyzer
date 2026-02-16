using System.Text.Json;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

/// <summary>
/// Loads and saves presets as JSON files in a presets directory next to the executable.
/// </summary>
public sealed class FilePresetRepository : IPresetRepository
{
    private static readonly JsonSerializerOptions s_readOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions s_writeOptions = new() { WriteIndented = true };

    private readonly string _presetsDirectory;

    public FilePresetRepository(string? presetsDirectory = null)
    {
        _presetsDirectory = presetsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "presets");
    }

    public IReadOnlyList<PresetInfo> GetAll()
    {
        EnsurePresetsDirectoryExists();
        var list = new List<PresetInfo>();
        foreach (var path in Directory.EnumerateFiles(_presetsDirectory, "*.json", SearchOption.TopDirectoryOnly))
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
        return File.Exists(path) ? LoadFromPath(id, path) : null;
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
        File.WriteAllText(path, json);
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
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void EnsurePresetsDirectoryExists()
    {
        if (!Directory.Exists(_presetsDirectory))
        {
            Directory.CreateDirectory(_presetsDirectory);
        }
    }

    private static Preset? LoadFromPath(string id, string path)
    {
        try
        {
            var json = File.ReadAllText(path);
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
