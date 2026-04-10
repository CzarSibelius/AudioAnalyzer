namespace AudioAnalyzer.Visualizers;

/// <summary>One expanding ripple in <see cref="BufferDistortionMode.DropRipples"/> mode; center is in layer-local pixels of the effect rectangle.</summary>
public sealed class BufferDistortionRipple
{
    /// <summary>Center X in layer-local coordinates (0..rect width).</summary>
    public float CenterX { get; set; }

    /// <summary>Center Y in layer-local coordinates (0..rect height).</summary>
    public float CenterY { get; set; }

    /// <summary>Seconds since this ripple was spawned.</summary>
    public double AgeSeconds { get; set; }
}
