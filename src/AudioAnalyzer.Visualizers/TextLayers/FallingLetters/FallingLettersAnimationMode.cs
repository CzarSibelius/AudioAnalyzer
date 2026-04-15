namespace AudioAnalyzer.Visualizers;

/// <summary>How the FallingLetters layer animates glyphs (particles vs column-style rain).</summary>
public enum FallingLettersAnimationMode
{
    /// <summary>Falling letter particles (default).</summary>
    Particles,

    /// <summary>Discrete falling characters per column (legacy MatrixRain-style).</summary>
    ColumnRain
}
