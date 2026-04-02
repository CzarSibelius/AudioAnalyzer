using System.Diagnostics;

namespace AudioAnalyzer.Application.Display;

/// <summary>
/// Rolling average FPS from intervals between completed main renders (ADR-0067).
/// </summary>
public sealed class MainRenderFpsMeter
{
    /// <summary>Number of frame intervals kept for smoothing.</summary>
    public const int DefaultWindowSize = 45;

    private readonly double[] _intervalSeconds;
    private int _writeIndex;
    private int _intervalCount;
    private long _lastTimestamp;
    private bool _haveLastTimestamp;

    /// <summary>
    /// Initializes a meter with the given ring buffer size for frame intervals.
    /// </summary>
    /// <param name="windowSize">Maximum number of stored intervals (must be at least 1).</param>
    public MainRenderFpsMeter(int windowSize = DefaultWindowSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(windowSize, 1, nameof(windowSize));
        _intervalSeconds = new double[windowSize];
    }

    /// <summary>Whether at least one frame interval has been recorded.</summary>
    public bool HasIntervalSample => _intervalCount > 0;

    /// <summary>
    /// Smoothed FPS as <c>count / sum(dt)</c> for consecutive frame intervals in seconds (ADR-0067).
    /// </summary>
    public static double ComputeSmoothedFpsFromIntervals(ReadOnlySpan<double> intervalSeconds)
    {
        if (intervalSeconds.Length == 0)
        {
            return 0;
        }

        double sum = 0;
        foreach (double t in intervalSeconds)
        {
            sum += t;
        }

        return sum > 0 ? intervalSeconds.Length / sum : 0;
    }

    /// <summary>
    /// Records the completion of a main render; updates the rolling interval buffer from the previous completion time.
    /// </summary>
    public void RecordFrameCompleted()
    {
        long now = Stopwatch.GetTimestamp();
        if (_haveLastTimestamp)
        {
            double seconds = (now - _lastTimestamp) / (double)Stopwatch.Frequency;
            if (seconds > 0)
            {
                _intervalSeconds[_writeIndex] = seconds;
                _writeIndex = (_writeIndex + 1) % _intervalSeconds.Length;
                if (_intervalCount < _intervalSeconds.Length)
                {
                    _intervalCount++;
                }
            }
        }

        _lastTimestamp = now;
        _haveLastTimestamp = true;
    }

    /// <summary>
    /// Smoothed FPS as (interval count) / (sum of stored intervals). Returns 0 if no samples.
    /// </summary>
    public double GetSmoothedFps()
    {
        if (_intervalCount == 0)
        {
            return 0;
        }

        Span<double> ordered = stackalloc double[_intervalSeconds.Length];
        int n = CopyIntervalsChronological(ordered);
        return ComputeSmoothedFpsFromIntervals(ordered[..n]);
    }

    private int CopyIntervalsChronological(Span<double> dest)
    {
        int n = _intervalCount;
        if (n < _intervalSeconds.Length)
        {
            _intervalSeconds.AsSpan(0, n).CopyTo(dest);
            return n;
        }

        for (int i = 0; i < n; i++)
        {
            dest[i] = _intervalSeconds[(_writeIndex + i) % _intervalSeconds.Length];
        }

        return n;
    }

    /// <summary>Clears stored intervals (e.g. after long pause). Does not reset the last timestamp.</summary>
    public void ClearIntervals()
    {
        _intervalCount = 0;
        _writeIndex = 0;
    }
}
