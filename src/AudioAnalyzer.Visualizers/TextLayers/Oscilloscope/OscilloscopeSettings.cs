using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Layer-specific settings for Oscilloscope. Only OscilloscopeLayer reads these.</summary>
public sealed class OscilloscopeSettings
{
    /// <summary>Amplitude gain (1.0–10.0). Default 2.5.</summary>
    [SettingRange(1.0, 10.0, 0.5)]
    public double Gain { get; set; } = 2.5;

    /// <summary>When true, fill the area between center line and waveform; when false, draw only the trace.</summary>
    public bool Filled { get; set; }
}
