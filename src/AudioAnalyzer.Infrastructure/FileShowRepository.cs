using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

/// <summary>
/// Loads and saves shows as JSON files in a shows directory next to the executable.
/// </summary>
public sealed class FileShowRepository : IShowRepository
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
    private readonly string _showsDirectory;

    /// <summary>Creates a repository using the real file system.</summary>
    public FileShowRepository(string? showsDirectory = null)
        : this(new FileSystem(), showsDirectory)
    {
    }

    /// <summary>Creates a repository using the provided file system (e.g. MockFileSystem for tests).</summary>
    public FileShowRepository(IFileSystem fileSystem, string? showsDirectory = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _showsDirectory = showsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shows");
    }

    public IReadOnlyList<ShowInfo> GetAll()
    {
        EnsureShowsDirectoryExists();
        var list = new List<ShowInfo>();
        foreach (var path in _fileSystem.Directory.EnumerateFiles(_showsDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            var id = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            var show = LoadFromPath(id, path);
            if (show != null)
            {
                var name = show.Name?.Trim();
                list.Add(new ShowInfo(id, string.IsNullOrEmpty(name) ? id : name));
            }
        }
        return list.OrderBy(s => s.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public Show? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        EnsureShowsDirectoryExists();
        var path = Path.Combine(_showsDirectory, id + ".json");
        return _fileSystem.File.Exists(path) ? LoadFromPath(id, path) : null;
    }

    public void Save(string id, Show show)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Show id cannot be null or empty.", nameof(id));
        }
        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Show id contains invalid filename characters.", nameof(id));
        }
        ArgumentNullException.ThrowIfNull(show);

        EnsureShowsDirectoryExists();
        var path = Path.Combine(_showsDirectory, id + ".json");
        var dto = new ShowFileDto
        {
            Name = show.Name,
            Entries = show.Entries?.Select(e => new ShowEntryDto
            {
                PresetId = e.PresetId,
                Duration = e.Duration == null ? null : new DurationConfigDto
                {
                    Unit = e.Duration.Unit,
                    Value = e.Duration.Value
                }
            }).ToList() ?? []
        };
        var json = JsonSerializer.Serialize(dto, s_writeOptions);
        _fileSystem.File.WriteAllText(path, json);
    }

    public string Create(Show show)
    {
        EnsureShowsDirectoryExists();
        var existing = GetAll();
        int n = 1;
        string id;
        do
        {
            id = $"show-{n}";
            n++;
        }
        while (existing.Any(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase)));

        Save(id, show);
        return id;
    }

    public void Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return;
        }
        var path = Path.Combine(_showsDirectory, id + ".json");
        if (_fileSystem.File.Exists(path))
        {
            _fileSystem.File.Delete(path);
        }
    }

    private void EnsureShowsDirectoryExists()
    {
        if (!_fileSystem.Directory.Exists(_showsDirectory))
        {
            _fileSystem.Directory.CreateDirectory(_showsDirectory);
        }
    }

    private Show? LoadFromPath(string id, string path)
    {
        try
        {
            var json = _fileSystem.File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<ShowFileDto>(json, s_readOptions);
            if (dto == null)
            {
                return null;
            }
            var entries = (dto.Entries ?? []).Select(e => new ShowEntry
            {
                PresetId = e.PresetId ?? "",
                Duration = e.Duration == null ? new DurationConfig() : new DurationConfig
                {
                    Unit = e.Duration.Unit,
                    Value = e.Duration.Value
                }
            }).ToList();
            return new Show
            {
                Id = id,
                Name = dto.Name ?? id,
                Entries = entries
            };
        }
        catch (Exception)
        {
            /* Skip invalid show file */
            return null;
        }
    }

    private sealed class ShowFileDto
    {
        public string? Name { get; set; }
        public List<ShowEntryDto>? Entries { get; set; }
    }

    private sealed class ShowEntryDto
    {
        public string? PresetId { get; set; }
        public DurationConfigDto? Duration { get; set; }
    }

    private sealed class DurationConfigDto
    {
        public DurationUnit Unit { get; set; }
        public double Value { get; set; }
    }
}
