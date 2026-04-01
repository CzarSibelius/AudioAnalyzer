namespace AudioAnalyzer.Domain;

/// <summary>
/// Where tempo and beat grid for the UI and layers come from. Spectrum/waveform/volume always follow the audio input path.
/// </summary>
public enum BpmSource
{
    /// <summary>Energy-based beat detection on the captured audio stream.</summary>
    AudioAnalysis = 0,

    /// <summary>Fixed BPM from the active <c>demo:</c> synthetic device id; beats derived from time.</summary>
    DemoDevice = 1,

    /// <summary>Ableton Link session (requires native <c>link_shim.dll</c>).</summary>
    AbletonLink = 2
}
