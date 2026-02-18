using System.IO.Abstractions;
using System.Text.Json;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

/// <summary>
/// Loads palettes from JSON files in a palettes directory next to the executable.
/// </summary>
public sealed class FilePaletteRepository : IPaletteRepository
{
    private readonly IFileSystem _fileSystem;
    private readonly string _palettesDirectory;

    /// <summary>Creates a repository using the real file system.</summary>
    public FilePaletteRepository(string? palettesDirectory = null)
        : this(new FileSystem(), palettesDirectory)
    {
    }

    /// <summary>Creates a repository using the provided file system (e.g. MockFileSystem for tests).</summary>
    public FilePaletteRepository(IFileSystem fileSystem, string? palettesDirectory = null)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _palettesDirectory = palettesDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "palettes");
    }

    public IReadOnlyList<PaletteInfo> GetAll()
    {
        EnsurePalettesDirectoryExists();
        var list = new List<PaletteInfo>();
        try
        {
            foreach (var path in _fileSystem.Directory.EnumerateFiles(_palettesDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var id = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                var def = LoadFromPath(path);
                var name = def?.Name?.Trim();
                list.Add(new PaletteInfo(id, string.IsNullOrEmpty(name) ? id : name));
            }
        }
        catch (IOException)
        {
            /* Directory missing or inaccessible: return empty list */
        }
        return list.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public PaletteDefinition? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        EnsurePalettesDirectoryExists();
        var path = Path.Combine(_palettesDirectory, id + ".json");
        return _fileSystem.File.Exists(path) ? LoadFromPath(path) : null;
    }

    private void EnsurePalettesDirectoryExists()
    {
        try
        {
            if (!_fileSystem.Directory.Exists(_palettesDirectory))
            {
                _fileSystem.Directory.CreateDirectory(_palettesDirectory);
            }
        }
        catch (IOException)
        {
            /* Best-effort: directory may not be creatable in test with read-only mock */
        }
    }

    private PaletteDefinition? LoadFromPath(string path)
    {
        try
        {
            var json = _fileSystem.File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var name = root.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() : null;
            var colors = new List<PaletteColorEntry>();
            if (root.TryGetProperty("Colors", out var colorsProp) && colorsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in colorsProp.EnumerateArray())
                {
                    if (el.ValueKind == JsonValueKind.String)
                    {
                        colors.Add(new PaletteColorEntry { Value = el.GetString() });
                        continue;
                    }
                    if (el.ValueKind == JsonValueKind.Object)
                    {
                        int? r = null, g = null, b = null;
                        if (el.TryGetProperty("R", out var rProp))
                        {
                            r = rProp.TryGetInt32(out var ri) ? ri : null;
                        }

                        if (el.TryGetProperty("G", out var gProp))
                        {
                            g = gProp.TryGetInt32(out var gi) ? gi : null;
                        }

                        if (el.TryGetProperty("B", out var bProp))
                        {
                            b = bProp.TryGetInt32(out var bi) ? bi : null;
                        }

                        if (r.HasValue || g.HasValue || b.HasValue)
                        {
                            colors.Add(new PaletteColorEntry { R = r, G = g, B = b });
                        }

                        continue;
                    }
                }
            }
            return new PaletteDefinition { Name = name, Colors = colors.ToArray() };
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
