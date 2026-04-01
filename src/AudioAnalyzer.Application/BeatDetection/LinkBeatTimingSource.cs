using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.BeatDetection;

/// <summary>
/// Beat timing from Ableton Link session (tempo + beat grid). Requires <see cref="ILinkSession.IsAvailable"/>.
/// </summary>
public sealed class LinkBeatTimingSource : IBeatTimingSource
{
    private const double DefaultQuantum = 4.0;
    private readonly ILinkSession _session;
    private int _lastWholeBeat = int.MinValue;
    private int _beatCount;
    private int _beatFlashFrames;
    private bool _hasWhole;

    /// <summary>Creates a timing source over the given Link session.</summary>
    public LinkBeatTimingSource(ILinkSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <inheritdoc />
    public double CurrentBpm { get; private set; }

    /// <inheritdoc />
    public int BeatCount => _beatCount;

    /// <inheritdoc />
    public bool BeatFlashActive => _beatFlashFrames > 0;

    /// <inheritdoc />
    public double BeatSensitivity
    {
        get => 1.3;
        set { /* Link timing ignores sensitivity */ }
    }

    /// <summary>Quantum in beats for phase (default 4).</summary>
    public double Quantum { get; set; } = DefaultQuantum;

    /// <summary>Called when entering Link BPM mode to seed beat tracking.</summary>
    public void ResetBeatTracking()
    {
        _lastWholeBeat = int.MinValue;
        _hasWhole = false;
        _beatCount = 0;
        _beatFlashFrames = 0;
    }

    /// <summary>Enables or disables Link networking when the native library is available.</summary>
    public void SetNetworkingEnabled(bool enabled)
    {
        if (_session.IsAvailable)
        {
            _session.SetEnabled(enabled);
        }
    }

    /// <inheritdoc />
    public void OnAudioFrame(double avgEnergy) => _ = avgEnergy;

    /// <inheritdoc />
    public void OnVisualTick()
    {
        if (!_session.IsAvailable || !_session.IsEnabled)
        {
            CurrentBpm = 0;
            DecayFlash();
            return;
        }

        _session.Capture(out double tempo, out _, out double beat, Quantum);
        CurrentBpm = tempo;

        int whole = (int)Math.Floor(beat);
        if (!_hasWhole)
        {
            _lastWholeBeat = whole;
            _hasWhole = true;
            DecayFlash();
            return;
        }

        if (whole > _lastWholeBeat)
        {
            int crossed = whole - _lastWholeBeat;
            _beatCount += crossed;
            _lastWholeBeat = whole;
            _beatFlashFrames = 3;
        }
        else if (whole < _lastWholeBeat)
        {
            // Session reset or join: resync without emitting many beats
            _lastWholeBeat = whole;
        }

        DecayFlash();
    }

    private void DecayFlash()
    {
        if (_beatFlashFrames > 0)
        {
            _beatFlashFrames--;
        }
    }
}
