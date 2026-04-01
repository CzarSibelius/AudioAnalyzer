using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.BeatDetection;

/// <summary>
/// Routes <see cref="IBeatTimingSource"/> to audio-derived, demo, or Link implementations.
/// </summary>
public sealed class BeatTimingRouter : IBeatTimingSource, IBeatTimingConfigurator
{
    private readonly AudioDerivedBeatTimingSource _audio;
    private readonly DemoBeatTimingSource _demo;
    private readonly LinkBeatTimingSource _link;
    private IBeatTimingSource _active;
    private BpmSource _source = BpmSource.AudioAnalysis;

    /// <summary>Creates a router with concrete timing backends.</summary>
    public BeatTimingRouter(
        AudioDerivedBeatTimingSource audio,
        DemoBeatTimingSource demo,
        LinkBeatTimingSource link)
    {
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        _demo = demo ?? throw new ArgumentNullException(nameof(demo));
        _link = link ?? throw new ArgumentNullException(nameof(link));
        _active = _audio;
    }

    /// <inheritdoc />
    public BpmSource ActiveBpmSource => _source;

    /// <inheritdoc />
    public void ApplyFromSettings(BpmSource source, string? deviceId)
    {
        _source = source;
        if (_source != BpmSource.AbletonLink)
        {
            _link.SetNetworkingEnabled(false);
            _link.ResetBeatTracking();
        }

        switch (_source)
        {
            case BpmSource.AudioAnalysis:
                _active = _audio;
                break;
            case BpmSource.DemoDevice:
                _demo.ConfigureFromDeviceId(deviceId);
                _active = _demo;
                break;
            case BpmSource.AbletonLink:
                _active = _link;
                _link.ResetBeatTracking();
                _link.SetNetworkingEnabled(true);
                break;
            default:
                _active = _audio;
                break;
        }
    }

    /// <inheritdoc />
    public double CurrentBpm => _active.CurrentBpm;

    /// <inheritdoc />
    public int BeatCount => _active.BeatCount;

    /// <inheritdoc />
    public bool BeatFlashActive => _active.BeatFlashActive;

    /// <inheritdoc />
    public double BeatSensitivity
    {
        get => _active.BeatSensitivity;
        set => _active.BeatSensitivity = value;
    }

    /// <inheritdoc />
    public void OnAudioFrame(double avgEnergy) => _active.OnAudioFrame(avgEnergy);

    /// <inheritdoc />
    public void OnVisualTick() => _active.OnVisualTick();
}
