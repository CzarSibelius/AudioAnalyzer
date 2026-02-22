namespace AudioAnalyzer.Visualizers;

/// <summary>State for Maschine layer: beat tracking, phase within cycle, and snippet selection.</summary>
public sealed class MaschineState
{
    /// <summary>Last beat count for phase advance detection.</summary>
    public int LastBeatCount { get; set; } = -1;

    /// <summary>Phase within the current cycle (0 .. text.Length-1). Number of lines shown is Phase + 1.</summary>
    public int Phase { get; set; }

    /// <summary>Snippet index for the current cycle; advanced when phase wraps to 0.</summary>
    public int SnippetIndex { get; set; }
}
