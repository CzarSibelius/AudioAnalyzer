namespace AudioAnalyzer.Visualizers;

/// <summary>How the accent (aligned diagonal) column is chosen in Maschine layer.</summary>
public enum MaschineAccentColumnMode
{
    /// <summary>Accent on the leftmost column of the cascade.</summary>
    Fixed,

    /// <summary>Accent column advances with beat phase so the highlight shifts each beat.</summary>
    Moving
}
