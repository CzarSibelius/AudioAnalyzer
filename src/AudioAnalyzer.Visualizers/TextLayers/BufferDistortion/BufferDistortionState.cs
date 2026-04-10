namespace AudioAnalyzer.Visualizers;

/// <summary>Per-slot animation state for <see cref="BufferDistortionLayer"/>.</summary>
public sealed class BufferDistortionState
{
    /// <summary>Phase accumulator for <see cref="BufferDistortionMode.PlaneWaves"/> (radians).</summary>
    public double PlanePhase { get; set; }

    /// <summary>Last beat count applied for ripple spawn; initialized lazily to avoid spawning on first frame.</summary>
    public int LastBeatCountForSpawn { get; set; } = int.MinValue;

    /// <summary>Active ripples in drop mode.</summary>
    public List<BufferDistortionRipple> Ripples { get; } = new();
}
