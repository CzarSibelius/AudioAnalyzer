using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.BeatDetection;

/// <summary>
/// Fixed BPM from a <c>demo:NNN</c> device id; beats and flash are derived from wall-clock time (not audio energy).
/// </summary>
public sealed class DemoBeatTimingSource : IBeatTimingSource
{
    private const double SensitivityPlaceholder = 1.3;
    private int _bpm = 120;
    private double _beatFraction;
    private int _beatCount;
    private int _beatFlashFrames;
    private DateTime _lastTick = DateTime.UtcNow;

    /// <inheritdoc />
    public double CurrentBpm => _bpm;

    /// <inheritdoc />
    public int BeatCount => _beatCount;

    /// <inheritdoc />
    public bool BeatFlashActive => _beatFlashFrames > 0;

    /// <inheritdoc />
    public double BeatSensitivity
    {
        get => SensitivityPlaceholder;
        set { /* demo timing ignores sensitivity */ }
    }

    /// <summary>Sets demo BPM and resets the beat phase (e.g. when switching devices).</summary>
    public void Configure(int bpm)
    {
        _bpm = Math.Clamp(bpm, 60, 180);
        _beatFraction = 0;
        _beatCount = 0;
        _beatFlashFrames = 0;
        _lastTick = DateTime.UtcNow;
    }

    /// <summary>Parses BPM from <paramref name="deviceId"/> or defaults to 120.</summary>
    public void ConfigureFromDeviceId(string? deviceId)
    {
        if (DemoAudioDevice.TryGetBpm(deviceId, out int parsed))
        {
            Configure(parsed);
        }
        else
        {
            Configure(120);
        }
    }

    /// <inheritdoc />
    public void OnAudioFrame(double avgEnergy) => _ = avgEnergy;

    /// <inheritdoc />
    public void OnVisualTick()
    {
        DateTime now = DateTime.UtcNow;
        double dt = (now - _lastTick).TotalSeconds;
        _lastTick = now;
        if (dt <= 0 || dt > 2.0)
        {
            return;
        }

        double beatsAdvanced = dt * (_bpm / 60.0);
        _beatFraction += beatsAdvanced;
        while (_beatFraction >= 1.0)
        {
            _beatFraction -= 1.0;
            _beatCount++;
            _beatFlashFrames = 3;
        }

        if (_beatFlashFrames > 0)
        {
            _beatFlashFrames--;
        }
    }
}
