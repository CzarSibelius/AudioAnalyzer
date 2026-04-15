using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

/// <summary>Loads and saves charset JSON files in a charsets directory next to the executable (ADR-0080).</summary>
public sealed class FileCharsetRepository : ICharsetRepository
{
    private const int MaxCharacters = 4096;

    private static readonly JsonSerializerOptions s_readOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions s_writeOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IFileSystem _fileSystem;
    private readonly string _charsetsDirectory;
    private readonly Dictionary<string, CharsetDefinition> _validById = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates a repository using the real file system.</summary>
    public FileCharsetRepository(string? charsetsDirectory = null)
        : this(new FileSystem(), charsetsDirectory)
    {
    }

    /// <summary>Creates a repository using the provided file system (e.g. tests).</summary>
    public FileCharsetRepository(IFileSystem fileSystem, string? charsetsDirectory = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _charsetsDirectory = charsetsDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "charsets");
    }

    /// <inheritdoc />
    public IReadOnlyList<CharsetInfo> GetAll()
    {
        EnsureCharsetsDirectoryExists();
        var list = new List<CharsetInfo>();
        try
        {
            foreach (var path in _fileSystem.Directory.EnumerateFiles(_charsetsDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var id = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var def = LoadFromPathUncached(path);
                if (def == null || string.IsNullOrEmpty(def.Characters))
                {
                    continue;
                }

                var name = def.Name?.Trim();
                list.Add(new CharsetInfo(id, string.IsNullOrEmpty(name) ? id : name));
            }
        }
        catch (IOException)
        {
            /* Directory missing or inaccessible */
        }

        return list.OrderBy(c => c.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <inheritdoc />
    public CharsetDefinition? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        string key = id.Trim();
        if (_validById.TryGetValue(key, out var cached))
        {
            return Clone(cached);
        }

        EnsureCharsetsDirectoryExists();
        var path = Path.Combine(_charsetsDirectory, key + ".json");
        if (!_fileSystem.File.Exists(path))
        {
            return null;
        }

        var def = LoadFromPathUncached(path);
        if (def != null && IsValid(def))
        {
            _validById[key] = Clone(def);
        }

        return def == null || !IsValid(def) ? null : Clone(def);
    }

    /// <inheritdoc />
    public void Save(string id, CharsetDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Charset id cannot be null or empty.", nameof(id));
        }

        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Charset id contains invalid filename characters.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(definition);
        if (!IsValid(definition))
        {
            throw new ArgumentException("Charset definition must have non-empty Characters within the allowed length.", nameof(definition));
        }

        EnsureCharsetsDirectoryExists();
        var path = Path.Combine(_charsetsDirectory, id.Trim() + ".json");
        var json = JsonSerializer.Serialize(definition, s_writeOptions);
        _fileSystem.File.WriteAllText(path, json);
        _validById[id.Trim()] = Clone(definition);
    }

    /// <inheritdoc />
    public string Create(CharsetDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!IsValid(definition))
        {
            throw new ArgumentException("Charset definition must have non-empty Characters within the allowed length.", nameof(definition));
        }

        EnsureCharsetsDirectoryExists();
        var existing = GetAll();
        int n = 1;
        string id;
        do
        {
            id = $"charset-{n}";
            n++;
        }
        while (existing.Any(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)));

        Save(id, definition);
        return id;
    }

    private static CharsetDefinition Clone(CharsetDefinition d) =>
        new() { Name = d.Name, Characters = d.Characters };

    private static bool IsValid(CharsetDefinition d) =>
        !string.IsNullOrEmpty(d.Characters) && d.Characters.Length <= MaxCharacters;

    private void EnsureCharsetsDirectoryExists()
    {
        try
        {
            if (!_fileSystem.Directory.Exists(_charsetsDirectory))
            {
                _fileSystem.Directory.CreateDirectory(_charsetsDirectory);
            }
        }
        catch (IOException)
        {
            /* Best effort */
        }
    }

    private CharsetDefinition? LoadFromPathUncached(string path)
    {
        try
        {
            var json = _fileSystem.File.ReadAllText(path);
            var def = JsonSerializer.Deserialize<CharsetDefinition>(json, s_readOptions);
            if (def == null || !IsValid(def))
            {
                return null;
            }

            return def;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
