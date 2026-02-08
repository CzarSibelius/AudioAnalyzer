namespace AudioAnalyzer.Domain;

/// <summary>
/// Palette as stored in a JSON file in the palettes directory. Serialization-friendly.
/// Name and ordered list of color entries (each entry is string or R,G,B per PaletteColorEntry).
/// </summary>
public class PaletteDefinition
{
    /// <summary>Display name (e.g. "Power Corruption & Lies").</summary>
    public string? Name { get; set; }

    /// <summary>Ordered list of color entries. Each entry is either a string (#RRGGBB or console color name) or R,G,B object.</summary>
    public PaletteColorEntry[] Colors { get; set; } = [];
}
