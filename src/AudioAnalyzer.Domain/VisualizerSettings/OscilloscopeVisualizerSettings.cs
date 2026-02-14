namespace AudioAnalyzer.Domain;

/// <summary>Settings for the Oscilloscope visualizer.</summary>
public class OscilloscopeVisualizerSettings
{
    /// <summary>Amplitude gain (1.0 = no boost, higher = more visible). Typically 1.0–10.0.</summary>
    public double Gain { get; set; } = 2.5;
}
