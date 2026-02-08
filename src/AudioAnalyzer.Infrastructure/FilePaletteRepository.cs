using System.Text.Json;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Infrastructure;

/// <summary>
/// Loads palettes from JSON files in a palettes directory next to the executable.
/// </summary>
public sealed class FilePaletteRepository : IPaletteRepository
{
    private readonly string _palettesDirectory;

    public FilePaletteRepository(string? palettesDirectory = null)
    {
        _palettesDirectory = palettesDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "palettes");
    }

    public IReadOnlyList<PaletteInfo> GetAll()
    {
        EnsurePalettesDirectoryExists();
        var list = new List<PaletteInfo>();
        try
        {
            foreach (var path in Directory.EnumerateFiles(_palettesDirectory, "*.json", SearchOption.TopDirectoryOnly))
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
        catch
        {
            // Return empty or whatever we have
        }
        return list.OrderBy(p => p.Id, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public PaletteDefinition? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }
        // Prevent path traversal
        if (id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return null;
        }

        EnsurePalettesDirectoryExists();
        var path = Path.Combine(_palettesDirectory, id + ".json");
        return File.Exists(path) ? LoadFromPath(path) : null;
    }

    private void EnsurePalettesDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_palettesDirectory))
            {
                Directory.CreateDirectory(_palettesDirectory);
            }
        }
        catch
        {
            // Ignore
        }
    }

    private static PaletteDefinition? LoadFromPath(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
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
        catch
        {
            return null;
        }
    }
}
