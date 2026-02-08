namespace AudioAnalyzer.Domain;

/// <summary>
/// One color entry in a palette file. Serialization-friendly: either a string (#RRGGBB or console color name) or R,G,B values.
/// Parsing from JSON is done in Infrastructure (custom converter); conversion to PaletteColor is done in Application.
/// </summary>
public class PaletteColorEntry
{
    /// <summary>When set, the color was given as a string: hex "#RRGGBB" or console color name (e.g. "Magenta").</summary>
    public string? Value { get; set; }

    /// <summary>Red 0–255 when color was given as RGB object.</summary>
    public int? R { get; set; }

    /// <summary>Green 0–255 when color was given as RGB object.</summary>
    public int? G { get; set; }

    /// <summary>Blue 0–255 when color was given as RGB object.</summary>
    public int? B { get; set; }
}
