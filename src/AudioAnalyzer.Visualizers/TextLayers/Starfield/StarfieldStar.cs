namespace AudioAnalyzer.Visualizers;

/// <summary>One star in model space for <see cref="StarfieldLayer"/>.</summary>
public readonly record struct StarfieldStar(double X, double Y, double Z, int GlyphIndex);
