using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Reconfigures beat timing when <see cref="AppSettings.BpmSource"/> or the audio device id changes.
/// </summary>
public interface IBeatTimingConfigurator
{
    /// <summary>Currently selected BPM source (mirrors persisted settings after <see cref="ApplyFromSettings"/>).</summary>
    BpmSource ActiveBpmSource { get; }

    /// <summary>
    /// Selects the active <see cref="IBeatTimingSource"/> implementation and updates demo BPM from <paramref name="deviceId"/> when needed.
    /// </summary>
    void ApplyFromSettings(BpmSource source, string? deviceId);
}
