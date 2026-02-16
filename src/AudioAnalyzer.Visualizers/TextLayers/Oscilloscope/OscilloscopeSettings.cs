namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for Oscilloscope. Only OscilloscopeLayer reads these.</summary>
public sealed class OscilloscopeSettings
{
    /// <summary>Amplitude gain (1.0–10.0). Default 2.5.</summary>
    public double Gain { get; set; } = 2.5;
}
