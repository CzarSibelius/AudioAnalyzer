namespace AudioAnalyzer.Visualizers;

/// <summary>How waveform strip columns pick colors.</summary>
public enum WaveformStripColorMode
{
    /// <summary>Distance from strip center maps to layer palette (oscilloscope-like).</summary>
    PaletteDistance,

    /// <summary>Approximate spectral coloring from overview band energies (Rekordbox-inspired).</summary>
    SpectralApprox,

    /// <summary>Frequency-resolved band coloring via Goertzel energy at fixed Hz per overview bucket (see ADR-0078).</summary>
    SpectralGoertzel
}
