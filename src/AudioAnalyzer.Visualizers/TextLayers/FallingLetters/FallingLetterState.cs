namespace AudioAnalyzer.Visualizers;

/// <summary>State for one falling letter: column, vertical position, character.</summary>
public struct FallingLetterState
{
    public int Col { get; set; }
    public double Y { get; set; }
    public char Character { get; set; }
}
