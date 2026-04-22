using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.BeatDetection;

/// <summary>Detects beats from audio energy using energy history and BPM estimation.</summary>
public sealed class BeatDetector : IBeatDetector
{
    /// <summary>
    /// Wall-clock window with no accepted beat after which smoothed BPM is cleared (see <c>specs/beat-timing-audio/spec.md</c>).
    /// </summary>
    public const double StaleBeatWindowSeconds = 6.0;

    private const int EnergyHistorySize = 20;
    private const int MinBeatInterval = 250;
    private const int BPMHistorySize = 8;

    private readonly Func<DateTime> _getUtcNow;
    private readonly Queue<double> _energyHistory = new();
    private readonly Queue<DateTime> _beatTimes = new();
    private double _beatThreshold = 1.3;
    private DateTime _lastBeatTime = DateTime.MinValue;
    private double _currentBpm;
    private int _beatFlashFrames;
    private int _beatCount;

    /// <summary>Creates a detector. <paramref name="getUtcNow"/> enables deterministic tests; production omits it.</summary>
    public BeatDetector(Func<DateTime>? getUtcNow = null)
    {
        _getUtcNow = getUtcNow ?? (() => DateTime.UtcNow);
    }

    /// <inheritdoc />
    public double BeatSensitivity
    {
        get => _beatThreshold;
        set => _beatThreshold = Math.Clamp(value, 0.5, 3.0);
    }

    /// <inheritdoc />
    public double CurrentBpm => _currentBpm;

    /// <inheritdoc />
    public bool BeatFlashActive => _beatFlashFrames > 0;

    /// <inheritdoc />
    public int BeatCount => _beatCount;

    /// <inheritdoc />
    public void ProcessFrame(double energy)
    {
        ExpireStaleTempoIfNeeded();

        _energyHistory.Enqueue(energy);
        if (_energyHistory.Count > EnergyHistorySize)
        {
            _energyHistory.Dequeue();
        }

        if (_energyHistory.Count < EnergyHistorySize / 2)
        {
            return;
        }

        double avgEnergy = _energyHistory.Take(_energyHistory.Count - 1).Average();
        DateTime now = _getUtcNow();
        if (energy > avgEnergy * _beatThreshold && energy > 0.01 &&
            (now - _lastBeatTime).TotalMilliseconds > MinBeatInterval)
        {
            _beatTimes.Enqueue(now);
            _lastBeatTime = now;
            _beatFlashFrames = 3;
            _beatCount++;
            while (_beatTimes.Count > 0 && (now - _beatTimes.Peek()).TotalSeconds > 8)
            {
                _beatTimes.Dequeue();
            }

            CalculateBPM();
        }
    }

    /// <inheritdoc />
    public void DecayFlashFrame()
    {
        ExpireStaleTempoIfNeeded();

        if (_beatFlashFrames > 0)
        {
            _beatFlashFrames--;
        }
    }

    /// <inheritdoc />
    public void ResetAudioDerivedBeatTiming()
    {
        _energyHistory.Clear();
        _beatTimes.Clear();
        _currentBpm = 0;
        _beatFlashFrames = 0;
        _beatCount = 0;
        _lastBeatTime = DateTime.MinValue;
    }

    private void ExpireStaleTempoIfNeeded()
    {
        if (_currentBpm <= 0)
        {
            return;
        }

        if (_lastBeatTime == DateTime.MinValue)
        {
            return;
        }

        DateTime now = _getUtcNow();
        if ((now - _lastBeatTime).TotalSeconds >= StaleBeatWindowSeconds)
        {
            ResetAudioDerivedBeatTiming();
        }
    }

    private void CalculateBPM()
    {
        if (_beatTimes.Count < 2)
        {
            return;
        }

        var recentBeats = _beatTimes.TakeLast(Math.Min(BPMHistorySize + 1, _beatTimes.Count)).ToList();
        if (recentBeats.Count < 2)
        {
            return;
        }

        var intervals = new List<double>();
        for (int i = 1; i < recentBeats.Count; i++)
        {
            double intervalMs = (recentBeats[i] - recentBeats[i - 1]).TotalMilliseconds;
            if (intervalMs >= 250 && intervalMs <= 2000)
            {
                intervals.Add(intervalMs);
            }
        }
        if (intervals.Count > 0)
        {
            double avgInterval = intervals.Average();
            double newBPM = 60000.0 / avgInterval;
            _currentBpm = _currentBpm == 0 ? newBPM : _currentBpm * 0.8 + newBPM * 0.2;
        }
    }
}
