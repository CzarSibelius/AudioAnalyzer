namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Immutable-by-convention copy of audio analysis state for one sample window: volume, beat timing, FFT bands, waveform, and channel peaks.
/// Produced by <see cref="AnalysisEngine.GetSnapshot"/>; not display layout or render instrumentation (see <see cref="VisualizationFrameContext"/>).
/// </summary>
public sealed class AudioAnalysisSnapshot
{
    public float Volume { get; set; }
    public double CurrentBpm { get; set; }
    public double BeatSensitivity { get; set; }
    public bool BeatFlashActive { get; set; }
    /// <summary>Incremented each time a beat is detected; visualizers can react to changes (e.g. spawn circles).</summary>
    public int BeatCount { get; set; }

    public int NumBands { get; set; }
    public double[] SmoothedMagnitudes { get; set; } = Array.Empty<double>();
    public double[] PeakHold { get; set; } = Array.Empty<double>();
    public double TargetMaxMagnitude { get; set; }

    public float[] Waveform { get; set; } = Array.Empty<float>();
    public int WaveformPosition { get; set; }
    public int WaveformSize { get; set; }

    /// <summary>Decimated waveform min per overview bucket (8192 buckets); empty when no history yet. Used by Waveform strip overview mode.</summary>
    public float[] WaveformOverviewMin { get; set; } = Array.Empty<float>();

    /// <summary>Decimated waveform max per overview bucket.</summary>
    public float[] WaveformOverviewMax { get; set; } = Array.Empty<float>();

    /// <summary>Approximate low-band energy per bucket (stylized spectral color).</summary>
    public float[] WaveformOverviewBandLow { get; set; } = Array.Empty<float>();

    /// <summary>Approximate mid-band energy per bucket (RMS).</summary>
    public float[] WaveformOverviewBandMid { get; set; } = Array.Empty<float>();

    /// <summary>Approximate high-band / transient energy per bucket.</summary>
    public float[] WaveformOverviewBandHigh { get; set; } = Array.Empty<float>();

    /// <summary>Goertzel low-band power per bucket (Hz from engine; used when <c>ColorMode == SpectralGoertzel</c>).</summary>
    public float[] WaveformOverviewBandLowGoertzel { get; set; } = Array.Empty<float>();

    /// <summary>Goertzel mid-band power per bucket.</summary>
    public float[] WaveformOverviewBandMidGoertzel { get; set; } = Array.Empty<float>();

    /// <summary>Goertzel high-band power per bucket.</summary>
    public float[] WaveformOverviewBandHighGoertzel { get; set; } = Array.Empty<float>();

    /// <summary>Right-channel Goertzel low-band power per bucket.</summary>
    public float[] WaveformOverviewBandLowGoertzelRight { get; set; } = Array.Empty<float>();

    /// <summary>Right-channel Goertzel mid-band power per bucket.</summary>
    public float[] WaveformOverviewBandMidGoertzelRight { get; set; } = Array.Empty<float>();

    /// <summary>Right-channel Goertzel high-band power per bucket.</summary>
    public float[] WaveformOverviewBandHighGoertzelRight { get; set; } = Array.Empty<float>();

    /// <summary>Right-channel overview min per bucket (same length as left when stereo ring is active).</summary>
    public float[] WaveformOverviewMinRight { get; set; } = Array.Empty<float>();

    /// <summary>Right-channel overview max per bucket.</summary>
    public float[] WaveformOverviewMaxRight { get; set; } = Array.Empty<float>();

    /// <summary>Right-channel low-band proxy or Goertzel low band per bucket.</summary>
    public float[] WaveformOverviewBandLowRight { get; set; } = Array.Empty<float>();

    /// <summary>Right-channel mid-band proxy or Goertzel mid band per bucket.</summary>
    public float[] WaveformOverviewBandMidRight { get; set; } = Array.Empty<float>();

    /// <summary>Right-channel high-band proxy or Goertzel high band per bucket.</summary>
    public float[] WaveformOverviewBandHighRight { get; set; } = Array.Empty<float>();

    /// <summary>Number of valid overview buckets (same length for all overview arrays when &gt; 0).</summary>
    public int WaveformOverviewLength { get; set; }

    /// <summary>Approximate wall time (seconds) spanned by the overview (oldest to newest in the ring).</summary>
    public double WaveformOverviewSpanSeconds { get; set; }

    /// <summary>Number of mono samples represented by the overview window (≤ ring capacity).</summary>
    public int WaveformOverviewValidSampleCount { get; set; }

    /// <summary>Global mono history write counter at newest sample in the overview (aligns beat marks).</summary>
    public long WaveformOverviewNewestMonoSampleIndex { get; set; }

    /// <summary>Global mono sample index of the oldest sample in the overview window.</summary>
    public long WaveformOverviewOldestMonoSampleIndex { get; set; }

    /// <summary>Valid mono sample count used when decimated overview arrays were last rebuilt (aligns beat/grid with pixels).</summary>
    public int WaveformOverviewBuiltValidSampleCount { get; set; }

    /// <summary><see cref="WaveformOverviewNewestMonoSampleIndex"/> at last overview rebuild.</summary>
    public long WaveformOverviewBuiltNewestMonoSampleIndex { get; set; }

    /// <summary>Oldest global mono index in the window represented by the last overview rebuild.</summary>
    public long WaveformOverviewBuiltOldestMonoSampleIndex { get; set; }

    /// <summary>
    /// Monotonic mono history sample index at each recorded beat (chronological with <see cref="WaveformBeatMarkBeatOrdinal"/>).
    /// </summary>
    public long[] WaveformBeatMarkMonoSampleIndex { get; set; } = Array.Empty<long>();

    /// <summary><see cref="BeatCount"/> value after each corresponding beat (for bar alignment).</summary>
    public int[] WaveformBeatMarkBeatOrdinal { get; set; } = Array.Empty<int>();

    /// <summary>Number of valid entries in beat-mark arrays.</summary>
    public int WaveformBeatMarkLength { get; set; }

    public float LeftChannel { get; set; }
    public float RightChannel { get; set; }
    public float LeftPeakHold { get; set; }
    public float RightPeakHold { get; set; }
}
