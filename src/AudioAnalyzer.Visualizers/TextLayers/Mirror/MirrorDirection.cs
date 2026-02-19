namespace AudioAnalyzer.Visualizers;

/// <summary>Which half of the buffer is the source; the other half is overwritten with its mirror.</summary>
public enum MirrorDirection
{
    /// <summary>Left half is source; right half shows mirror of left.</summary>
    LeftToRight,

    /// <summary>Right half is source; left half shows mirror of right.</summary>
    RightToLeft
}
