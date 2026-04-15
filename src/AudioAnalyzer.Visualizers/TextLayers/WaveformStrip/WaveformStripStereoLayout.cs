namespace AudioAnalyzer.Visualizers;

/// <summary>Whether the waveform strip renders a single mono overview or stacked left/right overviews.</summary>
/// <remarks>See <see cref="WaveformStripSettings.StereoLayout"/>.</remarks>
public enum WaveformStripStereoLayout
{
    /// <summary>Single strip from mono overview (default).</summary>
    Mono,

    /// <summary>Upper half = left overview, lower half = right overview (requires sufficient layer height).</summary>
    StereoStacked
}
