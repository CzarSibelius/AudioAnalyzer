namespace AudioAnalyzer.Domain;

/// <summary>
/// Reusable ordered collection of color names (e.g. for visualizers).
/// Serialization-friendly; parsing to ConsoleColor is done in Application/Infrastructure.
/// </summary>
public class ColorPalette
{
    /// <summary>Optional display name (e.g. "Power Corruption & Lies").</summary>
    public string? Name { get; set; }

    /// <summary>Ordered list of console color names (e.g. ["Magenta","Yellow","Green","Cyan","Blue"]).</summary>
    public string[] ColorNames { get; set; } = [];
}
