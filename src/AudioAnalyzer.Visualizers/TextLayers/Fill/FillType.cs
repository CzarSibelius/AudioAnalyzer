namespace AudioAnalyzer.Visualizers;

/// <summary>Character used to fill the viewport in the Fill layer.</summary>
public enum FillType
{
    /// <summary>Full block (█).</summary>
    FullBlock,

    /// <summary>Upper half block (▀).</summary>
    HalfBlockUpper,

    /// <summary>Lower half block (▄).</summary>
    HalfBlockLower,

    /// <summary>Light shade (░).</summary>
    LightShade,

    /// <summary>Medium shade (▒).</summary>
    MediumShade,

    /// <summary>Dark shade (▓).</summary>
    DarkShade,

    /// <summary>Space.</summary>
    Space,

    /// <summary>Custom character from CustomChar setting.</summary>
    Custom
}
