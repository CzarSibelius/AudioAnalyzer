using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Serialization;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

/// <summary>
/// Loads and saves UI themes as JSON files in a themes directory next to the executable.
/// </summary>
public sealed class FileUiThemeRepository : IUiThemeRepository
{
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
    private readonly string _themesDirectory;

    /// <summary>Creates a repository using the real file system.</summary>
    public FileUiThemeRepository(string? themesDirectory = null)
        : this(new FileSystem(), themesDirectory)
    {
    }

    /// <summary>Creates a repository using the provided file system (e.g. MockFileSystem for tests).</summary>
    public FileUiThemeRepository(IFileSystem fileSystem, string? themesDirectory = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _themesDirectory = themesDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "themes");
    }

    /// <inheritdoc />
    public IReadOnlyList<ThemeInfo> GetAll()
    {
        EnsureThemesDirectoryExists();
        var list = new List<ThemeInfo>();
        try
        {
            foreach (var path in _fileSystem.Directory.EnumerateFiles(_themesDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var id = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var theme = LoadFromPath(path);
                if (theme == null)
                {
                    continue;
                }

                var name = theme.Name?.Trim();
                string? fallback = string.IsNullOrWhiteSpace(theme.FallbackPaletteId)
                    ? null
                    : theme.FallbackPaletteId.Trim();
                list.Add(new ThemeInfo(id, string.IsNullOrEmpty(name) ? id : name, fallback));
            }
        }
        catch (IOException)
        {
            /* Directory missing or inaccessible */
        }

        return list.OrderBy(t => t.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <inheritdoc />
    public UiThemeDefinition? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        EnsureThemesDirectoryExists();
        var path = Path.Combine(_themesDirectory, id + ".json");
        return _fileSystem.File.Exists(path) ? LoadFromPath(path) : null;
    }

    /// <inheritdoc />
    public void Save(string id, UiThemeDefinition definition)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Theme id cannot be null or empty.", nameof(id));
        }

        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Theme id contains invalid filename characters.", nameof(id));
        }

        ArgumentNullException.ThrowIfNull(definition);

        EnsureThemesDirectoryExists();
        var path = Path.Combine(_themesDirectory, id + ".json");
        var json = JsonSerializer.Serialize(definition, s_writeOptions);
        _fileSystem.File.WriteAllText(path, json);
    }

    /// <inheritdoc />
    public string Create(UiThemeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        EnsureThemesDirectoryExists();
        var existing = GetAll();
        int n = 1;
        string id;
        do
        {
            id = $"theme-{n}";
            n++;
        }
        while (existing.Any(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase)));

        Save(id, definition);
        return id;
    }

    private void EnsureThemesDirectoryExists()
    {
        try
        {
            if (!_fileSystem.Directory.Exists(_themesDirectory))
            {
                _fileSystem.Directory.CreateDirectory(_themesDirectory);
            }
        }
        catch (IOException)
        {
            /* Best effort */
        }
    }

    private UiThemeDefinition? LoadFromPath(string path)
    {
        try
        {
            var json = _fileSystem.File.ReadAllText(path);
            return JsonSerializer.Deserialize<UiThemeDefinition>(json, s_readOptions);
        }
        catch
        {
            return null;
        }
    }
}
