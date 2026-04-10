namespace AudioAnalyzer.Visualizers;

/// <summary>How <see cref="BufferDistortionLayer"/> warps the cell buffer from layers below.</summary>
public enum BufferDistortionMode
{
    /// <summary>Expanding ring ripples from beat-spawned (or initial) impact points.</summary>
    DropRipples,

    /// <summary>Sinusoidal plane displacement (ocean-like or scan lines).</summary>
    PlaneWaves
}
