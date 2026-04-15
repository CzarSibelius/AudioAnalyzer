namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Applies user-configured mono waveform ring capacity for long history + decimated overview (ADR-0077).</summary>
public interface IWaveformHistoryConfigurator
{
    /// <summary>Reallocates the internal history ring from seconds and optional sample rate (Hz). Pass null to use last known or default 48000.</summary>
    void ApplyMaxHistorySeconds(double seconds, int? sampleRateHz);
}
