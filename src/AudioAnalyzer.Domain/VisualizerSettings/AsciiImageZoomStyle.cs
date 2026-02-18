namespace AudioAnalyzer.Domain;

/// <summary>Zoom animation style for the ASCII image layer.</summary>
public enum AsciiImageZoomStyle
{
    /// <summary>Sinusoidal zoom in/out.</summary>
    Sine,

    /// <summary>Ease-in-out style (slower at min/max).</summary>
    Breathe,

    /// <summary>Linear ramp up and down within range.</summary>
    PingPong
}
