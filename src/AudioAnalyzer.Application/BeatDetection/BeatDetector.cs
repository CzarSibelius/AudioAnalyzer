using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.BeatDetection;

/// <summary>Detects beats from audio energy using energy history and BPM estimation.</summary>
public sealed class BeatDetector : IBeatDetector
{
    private const int EnergyHistorySize = 20;
    private const int MinBeatInterval = 250;
    private const int BPMHistorySize = 8;

    private readonly Queue<double> _energyHistory = new();
    private readonly Queue<DateTime> _beatTimes = new();
    private double _beatThreshold = 1.3;
    private DateTime _lastBeatTime = DateTime.MinValue;
    private double _currentBpm;
    private int _beatFlashFrames;
    private int _beatCount;

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
        DateTime now = DateTime.Now;
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
        if (_beatFlashFrames > 0)
        {
            _beatFlashFrames--;
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
