namespace AudioAnalyzer.Domain;

/// <summary>Well-known charset file ids (filename without extension) shipped with the app.</summary>
public static class CharsetIds
{
    /// <summary>Classic ASCII luminance ramp (matches pre-0080 hardcoded default).</summary>
    public const string AsciiRampClassic = "ascii-ramp-classic";

    /// <summary>Density / plasma style steps (matches pre-0080 FractalZoom / Geiss).</summary>
    public const string DensitySoft = "density-soft";

    /// <summary>Unknown Pleasures layer ramp (matches pre-0080 constant).</summary>
    public const string UnknownPleasuresRamp = "unknown-pleasures-ramp";

    /// <summary>Default charset id for FallingLetters glyph pool when <c>CharsetId</c> is unset (shipped <c>digits.json</c>).</summary>
    public const string Digits = "digits";
}
