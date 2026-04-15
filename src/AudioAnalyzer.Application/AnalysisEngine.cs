using System;
using System.Diagnostics;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Fft;
using AudioAnalyzer.Application.VolumeAnalysis;

namespace AudioAnalyzer.Application;

/// <summary>
/// Performs analysis on incoming audio and exposes results via properties and GetSnapshot().
/// Callers/orchestrators are responsible for display dimensions, rendering, and header refresh.
/// </summary>
/// <remarks>
/// <see cref="ProcessAudio"/> may run on the capture thread while the UI thread calls <see cref="GetSnapshot"/>; a private lock
/// and per-call snapshot copies keep those paths thread-safe. Long waveform history uses an internal ring; snapshots expose a
/// small decimated overview (see ADR-0077) so GetSnapshot does not clone multi-megabyte buffers each frame.
/// </remarks>
public sealed class AnalysisEngine : IWaveformHistoryConfigurator, IWaveformRetainedHistoryReset
{
    private const int FftLength = 8192;
    private const int OscilloscopeRingSize = 512;
    private const int OverviewBucketCount = 8192;
    private const int MaxHistorySamplesCap = 5_000_000;
    private const int DefaultSampleRate = 48_000;
    private const int UpdateIntervalMs = 50;
    private const int BeatMarkCapacity = 512;
    private const int GoertzelScratchCap = 256;
    private const double GoertzelLowHz = 150.0;
    private const double GoertzelMidHz = 1_800.0;
    private const double GoertzelHighHz = 6_000.0;
    private static readonly int FftLog2N = (int)Math.Log2(FftLength);

    private readonly object _sync = new();

    private readonly ComplexFloat[] _fftBuffer = new ComplexFloat[FftLength];
    private int _bufferPosition;

    private readonly float[] _scopeBuffer = new float[OscilloscopeRingSize];
    private int _scopeWritePosition;
    private readonly float[] _displayScopeBuffer = new float[OscilloscopeRingSize];
    private int _displayScopeWritePosition;

    private float[] _historyRing = Array.Empty<float>();
    private float[] _historyRingRight = Array.Empty<float>();
    private int _historyCapacity;
    private int _historyWritePosition;
    private long _historyTotalWritten;

    private readonly float[] _overviewMin = new float[OverviewBucketCount];
    private readonly float[] _overviewMax = new float[OverviewBucketCount];
    private readonly float[] _overviewBandLow = new float[OverviewBucketCount];
    private readonly float[] _overviewBandMid = new float[OverviewBucketCount];
    private readonly float[] _overviewBandHigh = new float[OverviewBucketCount];
    private readonly float[] _overviewBandLowGoertzel = new float[OverviewBucketCount];
    private readonly float[] _overviewBandMidGoertzel = new float[OverviewBucketCount];
    private readonly float[] _overviewBandHighGoertzel = new float[OverviewBucketCount];

    private readonly float[] _overviewMinRight = new float[OverviewBucketCount];
    private readonly float[] _overviewMaxRight = new float[OverviewBucketCount];
    private readonly float[] _overviewBandLowRight = new float[OverviewBucketCount];
    private readonly float[] _overviewBandMidRight = new float[OverviewBucketCount];
    private readonly float[] _overviewBandHighRight = new float[OverviewBucketCount];
    private readonly float[] _overviewBandLowGoertzelRight = new float[OverviewBucketCount];
    private readonly float[] _overviewBandMidGoertzelRight = new float[OverviewBucketCount];
    private readonly float[] _overviewBandHighGoertzelRight = new float[OverviewBucketCount];

    private readonly float[] _bucketScratch = new float[GoertzelScratchCap];
    private readonly long[] _beatMarkGlobalSample = new long[BeatMarkCapacity];
    private readonly int[] _beatMarkBeatOrdinal = new int[BeatMarkCapacity];
    private int _beatWriteHead;
    private int _trackedBeatCountForMarks;

    private int _overviewFilledLength;
    private double _overviewSpanSeconds;
    private int _overviewBuiltValidSampleCount;
    private long _overviewBuiltNewestMonoSampleIndex;
    private long _overviewBuiltOldestMonoSampleIndex;

    private double _configuredHistorySeconds = 60.0;
    private int _lastKnownSampleRate = DefaultSampleRate;

    private long _lastUpdateTicks = Stopwatch.GetTimestamp();
    private long _lastBeatVisualPulseTicks;
    private int _numBands = 8;

    private readonly IBeatTimingSource _beatTiming;
    private readonly IVolumeAnalyzer _volumeAnalyzer;
    private readonly IFftBandProcessor _fftBandProcessor;
    private readonly IWaveformOverviewRebuildPolicy? _waveformOverviewRebuildPolicy;
    private readonly AudioAnalysisSnapshot _snapshot = new();

    /// <summary>Constructs the engine. Optional <paramref name="waveformOverviewRebuildPolicy"/> limits or skips decimated overview work (console supplies a preset-aware policy).</summary>
    public AnalysisEngine(
        IBeatTimingSource beatTiming,
        IVolumeAnalyzer volumeAnalyzer,
        IFftBandProcessor fftBandProcessor,
        IWaveformOverviewRebuildPolicy? waveformOverviewRebuildPolicy = null)
    {
        _beatTiming = beatTiming ?? throw new ArgumentNullException(nameof(beatTiming));
        _volumeAnalyzer = volumeAnalyzer ?? throw new ArgumentNullException(nameof(volumeAnalyzer));
        _fftBandProcessor = fftBandProcessor ?? throw new ArgumentNullException(nameof(fftBandProcessor));
        _waveformOverviewRebuildPolicy = waveformOverviewRebuildPolicy;
        _trackedBeatCountForMarks = beatTiming.BeatCount;
        ResizeHistoryRingFromConfiguredSeconds();
    }

    /// <summary>Mono sample count used for the last overview bucket partition (for tests / diagnostics).</summary>
    internal int LastOverviewRebuildPartitionMonoSampleCount { get; private set; }

    /// <summary>Beat detection sensitivity (0.5–3.0) when using audio-derived timing.</summary>
    public double BeatSensitivity { get => _beatTiming.BeatSensitivity; set => _beatTiming.BeatSensitivity = value; }

    /// <summary>Current BPM from the active beat timing source.</summary>
    public double CurrentBpm => _beatTiming.CurrentBpm;

    /// <summary>True when a beat was recently detected (used for visual flash effects).</summary>
    public bool BeatFlashActive => _beatTiming.BeatFlashActive;

    /// <summary>Latest volume from audio processing (0–1). Used for header display.</summary>
    public float Volume => _volumeAnalyzer.Volume;

    /// <summary>Incremented each time a beat is detected. Used for Show playback with beats duration.</summary>
    public int BeatCount => _beatTiming.BeatCount;

    /// <inheritdoc />
    public void ApplyMaxHistorySeconds(double seconds, int? sampleRateHz)
    {
        lock (_sync)
        {
            _configuredHistorySeconds = ClampHistorySeconds(seconds);
            if (sampleRateHz is > 0)
            {
                _lastKnownSampleRate = sampleRateHz.Value;
            }

            ResizeHistoryRingFromConfiguredSeconds();
        }
    }

    /// <inheritdoc />
    public void ResetRetainedWaveformHistory()
    {
        lock (_sync)
        {
            ResetRetainedWaveformHistoryCore();
        }
    }

    /// <summary>
    /// Sets the number of FFT bands to compute. Call from the orchestrator when display dimensions change.
    /// </summary>
    public void SetNumBands(int numBands)
    {
        lock (_sync)
        {
            _numBands = Math.Max(8, Math.Min(60, numBands));
        }
    }

    /// <summary>
    /// Advances beat flash decay and external clocks (Demo/Link) at most once per ~50 ms.
    /// Called from audio processing and header refresh so Link advances without duplicating pulses.
    /// </summary>
    public void PulseBeatVisualIfDue()
    {
        lock (_sync)
        {
            PulseBeatVisualIfDueCore();
        }
    }

    /// <summary>
    /// Returns a copy of the current <see cref="AudioAnalysisSnapshot"/> with array properties cloned so callers can
    /// use it without racing <see cref="ProcessAudio"/>. Wrap in <see cref="VisualizationFrameContext"/> for layout and timing (orchestrator).
    /// </summary>
    /// <remarks>Clone paths use GC.AllocateUninitializedArray for numeric buffers where applicable to avoid redundant zero-init (ADR-0030).</remarks>
    public AudioAnalysisSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            FillSnapshot();
            return CloneSnapshotArrays(_snapshot);
        }
    }

    /// <summary>
    /// Processes incoming audio: volume, waveform, FFT bands, beat detection. Updates internal state and
    /// the display waveform at UpdateIntervalMs. Does not perform any rendering or header refresh.
    /// </summary>
    public void ProcessAudio(byte[] buffer, int bytesRecorded, AudioFormat format)
    {
        lock (_sync)
        {
            ProcessAudioCore(buffer, bytesRecorded, format);
        }
    }

    private void ProcessAudioCore(byte[] buffer, int bytesRecorded, AudioFormat format)
    {
        if (format.SampleRate > 0 && format.SampleRate != _lastKnownSampleRate)
        {
            _lastKnownSampleRate = format.SampleRate;
            ResizeHistoryRingFromConfiguredSeconds();
        }

        int bytesPerFrame = format.BytesPerFrame;
        int framesRecorded = bytesRecorded / bytesPerFrame;
        int bytesPerSample = format.BytesPerSample;
        int channels = format.Channels;

        float maxVolume = 0;
        float maxLeft = 0, maxRight = 0;
        double instantEnergy = 0;

        for (int frame = 0; frame < framesRecorded; frame++)
        {
            int frameOffset = frame * bytesPerFrame;
            float left = format.BitsPerSample switch
            {
                16 => BitConverter.ToInt16(buffer, frameOffset) / 32768f,
                32 => BitConverter.ToSingle(buffer, frameOffset),
                _ => 0
            };
            float right = channels >= 2
                ? format.BitsPerSample switch
                {
                    16 => BitConverter.ToInt16(buffer, frameOffset + bytesPerSample) / 32768f,
                    32 => BitConverter.ToSingle(buffer, frameOffset + bytesPerSample),
                    _ => 0
                }
                : left;

            float mono = (left + right) / 2;
            maxVolume = Math.Max(maxVolume, Math.Abs(mono));
            maxLeft = Math.Max(maxLeft, Math.Abs(left));
            maxRight = Math.Max(maxRight, Math.Abs(right));
            instantEnergy += mono * mono;

            _scopeBuffer[_scopeWritePosition] = mono;
            _scopeWritePosition = (_scopeWritePosition + 1) % OscilloscopeRingSize;

            if (_historyCapacity > 0)
            {
                _historyRing[_historyWritePosition] = mono;
                _historyRingRight[_historyWritePosition] = right;
                _historyWritePosition = (_historyWritePosition + 1) % _historyCapacity;
                _historyTotalWritten++;
            }

            if (_bufferPosition < FftLength)
            {
                _fftBuffer[_bufferPosition].X = mono;
                _fftBuffer[_bufferPosition].Y = 0;
                _bufferPosition++;
            }
        }

        _volumeAnalyzer.ProcessFrame(maxLeft, maxRight, maxVolume);

        if (_bufferPosition >= FftLength)
        {
            ApplyWindow();
            FftHelper.Fft(true, FftLog2N, _fftBuffer);
            _fftBandProcessor.Process(_fftBuffer, format.SampleRate, _numBands);
            _bufferPosition = 0;
        }

        double avgEnergy = framesRecorded > 0 ? Math.Sqrt(instantEnergy / framesRecorded) : 0;
        _beatTiming.OnAudioFrame(avgEnergy);
        SyncBeatMarksWithTiming();

        long nowTicks = Stopwatch.GetTimestamp();
        if ((nowTicks - _lastUpdateTicks) * 1000.0 / Stopwatch.Frequency >= UpdateIntervalMs)
        {
            Array.Copy(_scopeBuffer, _displayScopeBuffer, OscilloscopeRingSize);
            _displayScopeWritePosition = _scopeWritePosition;
            RebuildWaveformOverview();
            _lastUpdateTicks = nowTicks;
            PulseBeatVisualIfDueCore();
        }
    }

    private void PulseBeatVisualIfDueCore()
    {
        long nowTicks = Stopwatch.GetTimestamp();
        if ((nowTicks - _lastBeatVisualPulseTicks) * 1000.0 / Stopwatch.Frequency < UpdateIntervalMs)
        {
            return;
        }

        _lastBeatVisualPulseTicks = nowTicks;
        _beatTiming.OnVisualTick();
        SyncBeatMarksWithTiming();
    }

    private void SyncBeatMarksWithTiming()
    {
        int bc = _beatTiming.BeatCount;
        if (bc < _trackedBeatCountForMarks)
        {
            _beatWriteHead = 0;
            _trackedBeatCountForMarks = bc;
            return;
        }

        while (_trackedBeatCountForMarks < bc)
        {
            _trackedBeatCountForMarks++;
            int slot = _beatWriteHead % BeatMarkCapacity;
            _beatMarkGlobalSample[slot] = _historyTotalWritten;
            _beatMarkBeatOrdinal[slot] = _trackedBeatCountForMarks;
            _beatWriteHead++;
        }
    }

    private void ResizeHistoryRingFromConfiguredSeconds()
    {
        int target = (int)Math.Clamp(
            (long)_lastKnownSampleRate * (long)Math.Round(_configuredHistorySeconds),
            OscilloscopeRingSize,
            MaxHistorySamplesCap);
        if (target == _historyCapacity && _historyRing.Length == target)
        {
            return;
        }

        _historyRing = new float[target];
        _historyRingRight = new float[target];
        _historyCapacity = target;
        _historyWritePosition = 0;
        _historyTotalWritten = 0;
        _beatWriteHead = 0;
        _trackedBeatCountForMarks = _beatTiming.BeatCount;
        ClearOverview();
    }

    private void ResetRetainedWaveformHistoryCore()
    {
        if (_historyCapacity > 0 && _historyRing.Length >= _historyCapacity && _historyRingRight.Length >= _historyCapacity)
        {
            Array.Clear(_historyRing, 0, _historyCapacity);
            Array.Clear(_historyRingRight, 0, _historyCapacity);
        }

        _historyWritePosition = 0;
        _historyTotalWritten = 0;
        _beatWriteHead = 0;
        _trackedBeatCountForMarks = _beatTiming.BeatCount;
        ClearOverview();

        Array.Clear(_scopeBuffer);
        Array.Clear(_displayScopeBuffer);
        _scopeWritePosition = 0;
        _displayScopeWritePosition = 0;
    }

    private static double ClampHistorySeconds(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
        {
            return 60.0;
        }

        return Math.Clamp(seconds, 5.0, 180.0);
    }

    private void ClearOverview()
    {
        _overviewFilledLength = 0;
        _overviewSpanSeconds = 0;
        _overviewBuiltValidSampleCount = 0;
        _overviewBuiltNewestMonoSampleIndex = 0;
        _overviewBuiltOldestMonoSampleIndex = 0;
        LastOverviewRebuildPartitionMonoSampleCount = 0;
    }

    private void RebuildWaveformOverview()
    {
        long validSamplesLong = Math.Min(_historyTotalWritten, _historyCapacity);
        if (validSamplesLong < 2 || _historyCapacity <= 0 || _lastKnownSampleRate <= 0)
        {
            ClearOverview();
            return;
        }

        int n = (int)validSamplesLong;
        WaveformOverviewRebuildDecision decision = _waveformOverviewRebuildPolicy is null
            ? WaveformOverviewRebuildDecision.FullRing()
            : _waveformOverviewRebuildPolicy.GetDecision(n, _lastKnownSampleRate);

        if (decision.Mode == WaveformOverviewRebuildMode.Skip)
        {
            ClearOverview();
            return;
        }

        int partitionN = decision.Mode == WaveformOverviewRebuildMode.TrailingWindow
            ? Math.Clamp(decision.TrailingMonoSamples, 2, n)
            : n;
        int tailStart = n - partitionN;
        int bCount = OverviewBucketCount;
        double sr = _lastKnownSampleRate;
        for (int b = 0; b < bCount; b++)
        {
            WaveformOverviewBucketIndex.GetBucketSampleRange(b, partitionN, bCount, out int w0, out int w1);
            int t0 = tailStart + w0;
            int t1 = tailStart + w1;

            FillBucketAggregates(_historyRing, n, t0, t1, b, _overviewMin, _overviewMax, _overviewBandLow, _overviewBandMid, _overviewBandHigh, _overviewBandLowGoertzel, _overviewBandMidGoertzel, _overviewBandHighGoertzel, sr);
            FillBucketAggregates(_historyRingRight, n, t0, t1, b, _overviewMinRight, _overviewMaxRight, _overviewBandLowRight, _overviewBandMidRight, _overviewBandHighRight, _overviewBandLowGoertzelRight, _overviewBandMidGoertzelRight, _overviewBandHighGoertzelRight, sr);
        }

        _overviewFilledLength = bCount;
        _overviewSpanSeconds = partitionN / (double)_lastKnownSampleRate;
        _overviewBuiltValidSampleCount = partitionN;
        _overviewBuiltNewestMonoSampleIndex = _historyTotalWritten;
        _overviewBuiltOldestMonoSampleIndex = _historyTotalWritten - partitionN + 1;
        LastOverviewRebuildPartitionMonoSampleCount = partitionN;
    }

    private void FillBucketAggregates(
        float[] ring,
        int validCount,
        int t0,
        int t1,
        int bucketIndex,
        float[] minOut,
        float[] maxOut,
        float[] lowHeu,
        float[] midHeu,
        float[] hiHeu,
        float[] lowG,
        float[] midG,
        float[] hiG,
        double sampleRate)
    {
        float minV = float.MaxValue;
        float maxV = float.MinValue;
        double sumSq = 0;
        double sumAbs = 0;
        float maxDiffSq = 0;
        float prev = SampleChronological(ring, validCount, t0);
        int count = 0;
        int scratchCount = 0;
        for (int t = t0; t < t1; t++)
        {
            float s = SampleChronological(ring, validCount, t);
            minV = Math.Min(minV, s);
            maxV = Math.Max(maxV, s);
            sumSq += s * s;
            sumAbs += Math.Abs(s);
            if (t > t0)
            {
                float d = s - prev;
                maxDiffSq = Math.Max(maxDiffSq, d * d);
            }

            prev = s;
            count++;
            if (scratchCount < GoertzelScratchCap)
            {
                _bucketScratch[scratchCount++] = s;
            }
        }

        if (count == 0)
        {
            minV = maxV = 0;
        }

        minOut[bucketIndex] = minV;
        maxOut[bucketIndex] = maxV;
        lowHeu[bucketIndex] = count > 0 ? (float)(sumAbs / count) : 0f;
        midHeu[bucketIndex] = count > 0 ? MathF.Sqrt((float)(sumSq / count)) : 0f;
        hiHeu[bucketIndex] = MathF.Sqrt(maxDiffSq);

        ReadOnlySpan<float> span = _bucketScratch.AsSpan(0, scratchCount);
        lowG[bucketIndex] = GoertzelHelper.BandPower(span, GoertzelLowHz, sampleRate);
        midG[bucketIndex] = GoertzelHelper.BandPower(span, GoertzelMidHz, sampleRate);
        hiG[bucketIndex] = GoertzelHelper.BandPower(span, GoertzelHighHz, sampleRate);
    }

    private float SampleChronological(float[] ring, int validCount, int chronologicalIndex)
    {
        if (_historyTotalWritten < _historyCapacity)
        {
            return ring[chronologicalIndex];
        }

        return ring[(_historyWritePosition + chronologicalIndex) % _historyCapacity];
    }

    private void FillSnapshot()
    {
        SyncBeatMarksWithTiming();
        _snapshot.Volume = _volumeAnalyzer.Volume;
        _snapshot.CurrentBpm = _beatTiming.CurrentBpm;
        _snapshot.BeatSensitivity = _beatTiming.BeatSensitivity;
        _snapshot.BeatFlashActive = _beatTiming.BeatFlashActive;
        _snapshot.BeatCount = _beatTiming.BeatCount;
        _snapshot.NumBands = _fftBandProcessor.NumBands;
        _snapshot.SmoothedMagnitudes = _fftBandProcessor.SmoothedMagnitudes;
        _snapshot.PeakHold = _fftBandProcessor.PeakHold;
        _snapshot.TargetMaxMagnitude = _fftBandProcessor.TargetMaxMagnitude;
        _snapshot.Waveform = _displayScopeBuffer;
        _snapshot.WaveformPosition = _displayScopeWritePosition;
        _snapshot.WaveformSize = OscilloscopeRingSize;
        _snapshot.LeftChannel = _volumeAnalyzer.LeftChannel;
        _snapshot.RightChannel = _volumeAnalyzer.RightChannel;
        _snapshot.LeftPeakHold = _volumeAnalyzer.LeftPeakHold;
        _snapshot.RightPeakHold = _volumeAnalyzer.RightPeakHold;

        if (_overviewFilledLength > 0)
        {
            _snapshot.WaveformOverviewMin = _overviewMin;
            _snapshot.WaveformOverviewMax = _overviewMax;
            _snapshot.WaveformOverviewBandLow = _overviewBandLow;
            _snapshot.WaveformOverviewBandMid = _overviewBandMid;
            _snapshot.WaveformOverviewBandHigh = _overviewBandHigh;
            _snapshot.WaveformOverviewBandLowGoertzel = _overviewBandLowGoertzel;
            _snapshot.WaveformOverviewBandMidGoertzel = _overviewBandMidGoertzel;
            _snapshot.WaveformOverviewBandHighGoertzel = _overviewBandHighGoertzel;
            _snapshot.WaveformOverviewMinRight = _overviewMinRight;
            _snapshot.WaveformOverviewMaxRight = _overviewMaxRight;
            _snapshot.WaveformOverviewBandLowRight = _overviewBandLowRight;
            _snapshot.WaveformOverviewBandMidRight = _overviewBandMidRight;
            _snapshot.WaveformOverviewBandHighRight = _overviewBandHighRight;
            _snapshot.WaveformOverviewBandLowGoertzelRight = _overviewBandLowGoertzelRight;
            _snapshot.WaveformOverviewBandMidGoertzelRight = _overviewBandMidGoertzelRight;
            _snapshot.WaveformOverviewBandHighGoertzelRight = _overviewBandHighGoertzelRight;
            _snapshot.WaveformOverviewLength = _overviewFilledLength;
            _snapshot.WaveformOverviewSpanSeconds = _overviewSpanSeconds;
            _snapshot.WaveformOverviewValidSampleCount = _overviewBuiltValidSampleCount;
            _snapshot.WaveformOverviewNewestMonoSampleIndex = _historyTotalWritten;
            _snapshot.WaveformOverviewOldestMonoSampleIndex = _overviewBuiltOldestMonoSampleIndex;
            _snapshot.WaveformOverviewBuiltValidSampleCount = _overviewBuiltValidSampleCount;
            _snapshot.WaveformOverviewBuiltNewestMonoSampleIndex = _overviewBuiltNewestMonoSampleIndex;
            _snapshot.WaveformOverviewBuiltOldestMonoSampleIndex = _overviewBuiltOldestMonoSampleIndex;
        }
        else
        {
            _snapshot.WaveformOverviewMin = Array.Empty<float>();
            _snapshot.WaveformOverviewMax = Array.Empty<float>();
            _snapshot.WaveformOverviewBandLow = Array.Empty<float>();
            _snapshot.WaveformOverviewBandMid = Array.Empty<float>();
            _snapshot.WaveformOverviewBandHigh = Array.Empty<float>();
            _snapshot.WaveformOverviewBandLowGoertzel = Array.Empty<float>();
            _snapshot.WaveformOverviewBandMidGoertzel = Array.Empty<float>();
            _snapshot.WaveformOverviewBandHighGoertzel = Array.Empty<float>();
            _snapshot.WaveformOverviewMinRight = Array.Empty<float>();
            _snapshot.WaveformOverviewMaxRight = Array.Empty<float>();
            _snapshot.WaveformOverviewBandLowRight = Array.Empty<float>();
            _snapshot.WaveformOverviewBandMidRight = Array.Empty<float>();
            _snapshot.WaveformOverviewBandHighRight = Array.Empty<float>();
            _snapshot.WaveformOverviewBandLowGoertzelRight = Array.Empty<float>();
            _snapshot.WaveformOverviewBandMidGoertzelRight = Array.Empty<float>();
            _snapshot.WaveformOverviewBandHighGoertzelRight = Array.Empty<float>();
            _snapshot.WaveformOverviewLength = 0;
            _snapshot.WaveformOverviewSpanSeconds = 0;
            _snapshot.WaveformOverviewValidSampleCount = 0;
            _snapshot.WaveformOverviewNewestMonoSampleIndex = 0;
            _snapshot.WaveformOverviewOldestMonoSampleIndex = 0;
            _snapshot.WaveformOverviewBuiltValidSampleCount = 0;
            _snapshot.WaveformOverviewBuiltNewestMonoSampleIndex = 0;
            _snapshot.WaveformOverviewBuiltOldestMonoSampleIndex = 0;
        }

        FillBeatMarksSnapshot();
    }

    private void FillBeatMarksSnapshot()
    {
        long validSamplesLong = Math.Min(_historyTotalWritten, _historyCapacity);
        if (validSamplesLong < 1 || _beatWriteHead == 0)
        {
            _snapshot.WaveformBeatMarkMonoSampleIndex = Array.Empty<long>();
            _snapshot.WaveformBeatMarkBeatOrdinal = Array.Empty<int>();
            _snapshot.WaveformBeatMarkLength = 0;
            return;
        }

        long newestGlobal = _historyTotalWritten;
        long oldestGlobal = _historyTotalWritten < _historyCapacity
            ? 1
            : newestGlobal - validSamplesLong + 1;

        int h = _beatWriteHead;
        int n = Math.Min(BeatMarkCapacity, h);
        var tmpS = new long[BeatMarkCapacity];
        var tmpO = new int[BeatMarkCapacity];
        int m = 0;
        for (int k = 0; k < n; k++)
        {
            int slot = ((h - n + k) % BeatMarkCapacity + BeatMarkCapacity) % BeatMarkCapacity;
            long g = _beatMarkGlobalSample[slot];
            if (g >= oldestGlobal && g <= newestGlobal)
            {
                tmpS[m] = g;
                tmpO[m] = _beatMarkBeatOrdinal[slot];
                m++;
            }
        }

        if (m == 0)
        {
            _snapshot.WaveformBeatMarkMonoSampleIndex = Array.Empty<long>();
            _snapshot.WaveformBeatMarkBeatOrdinal = Array.Empty<int>();
            _snapshot.WaveformBeatMarkLength = 0;
            return;
        }

        var sa = new long[m];
        var oa = new int[m];
        Array.Copy(tmpS, sa, m);
        Array.Copy(tmpO, oa, m);
        _snapshot.WaveformBeatMarkMonoSampleIndex = sa;
        _snapshot.WaveformBeatMarkBeatOrdinal = oa;
        _snapshot.WaveformBeatMarkLength = m;
    }

    private static AudioAnalysisSnapshot CloneSnapshotArrays(AudioAnalysisSnapshot source)
    {
        double[] sm = source.SmoothedMagnitudes ?? Array.Empty<double>();
        double[] ph = source.PeakHold ?? Array.Empty<double>();
        float[] wf = source.Waveform ?? Array.Empty<float>();

        var smCopy = sm.Length == 0 ? Array.Empty<double>() : GC.AllocateUninitializedArray<double>(sm.Length);
        if (sm.Length > 0)
        {
            Array.Copy(sm, smCopy, sm.Length);
        }

        var phCopy = ph.Length == 0 ? Array.Empty<double>() : GC.AllocateUninitializedArray<double>(ph.Length);
        if (ph.Length > 0)
        {
            Array.Copy(ph, phCopy, ph.Length);
        }

        var wfCopy = wf.Length == 0 ? Array.Empty<float>() : GC.AllocateUninitializedArray<float>(wf.Length);
        if (wf.Length > 0)
        {
            Array.Copy(wf, wfCopy, wf.Length);
        }

        int ovLen = source.WaveformOverviewLength;
        float[] ovMin = CopyFloatSegment(source.WaveformOverviewMin, ovLen);
        float[] ovMax = CopyFloatSegment(source.WaveformOverviewMax, ovLen);
        float[] ovLo = CopyFloatSegment(source.WaveformOverviewBandLow, ovLen);
        float[] ovMid = CopyFloatSegment(source.WaveformOverviewBandMid, ovLen);
        float[] ovHi = CopyFloatSegment(source.WaveformOverviewBandHigh, ovLen);
        float[] ovLoG = CopyFloatSegment(source.WaveformOverviewBandLowGoertzel, ovLen);
        float[] ovMidG = CopyFloatSegment(source.WaveformOverviewBandMidGoertzel, ovLen);
        float[] ovHiG = CopyFloatSegment(source.WaveformOverviewBandHighGoertzel, ovLen);
        float[] ovMinR = CopyFloatSegment(source.WaveformOverviewMinRight, ovLen);
        float[] ovMaxR = CopyFloatSegment(source.WaveformOverviewMaxRight, ovLen);
        float[] ovLoR = CopyFloatSegment(source.WaveformOverviewBandLowRight, ovLen);
        float[] ovMidR = CopyFloatSegment(source.WaveformOverviewBandMidRight, ovLen);
        float[] ovHiR = CopyFloatSegment(source.WaveformOverviewBandHighRight, ovLen);
        float[] ovLoGR = CopyFloatSegment(source.WaveformOverviewBandLowGoertzelRight, ovLen);
        float[] ovMidGR = CopyFloatSegment(source.WaveformOverviewBandMidGoertzelRight, ovLen);
        float[] ovHiGR = CopyFloatSegment(source.WaveformOverviewBandHighGoertzelRight, ovLen);

        int bmLen = source.WaveformBeatMarkLength;
        long[] bmS = CopyLongSegment(source.WaveformBeatMarkMonoSampleIndex, bmLen);
        int[] bmO = CopyIntSegment(source.WaveformBeatMarkBeatOrdinal, bmLen);

        return new AudioAnalysisSnapshot
        {
            Volume = source.Volume,
            CurrentBpm = source.CurrentBpm,
            BeatSensitivity = source.BeatSensitivity,
            BeatFlashActive = source.BeatFlashActive,
            BeatCount = source.BeatCount,
            NumBands = source.NumBands,
            SmoothedMagnitudes = smCopy,
            PeakHold = phCopy,
            TargetMaxMagnitude = source.TargetMaxMagnitude,
            Waveform = wfCopy,
            WaveformPosition = source.WaveformPosition,
            WaveformSize = source.WaveformSize,
            WaveformOverviewMin = ovMin,
            WaveformOverviewMax = ovMax,
            WaveformOverviewBandLow = ovLo,
            WaveformOverviewBandMid = ovMid,
            WaveformOverviewBandHigh = ovHi,
            WaveformOverviewBandLowGoertzel = ovLoG,
            WaveformOverviewBandMidGoertzel = ovMidG,
            WaveformOverviewBandHighGoertzel = ovHiG,
            WaveformOverviewMinRight = ovMinR,
            WaveformOverviewMaxRight = ovMaxR,
            WaveformOverviewBandLowRight = ovLoR,
            WaveformOverviewBandMidRight = ovMidR,
            WaveformOverviewBandHighRight = ovHiR,
            WaveformOverviewBandLowGoertzelRight = ovLoGR,
            WaveformOverviewBandMidGoertzelRight = ovMidGR,
            WaveformOverviewBandHighGoertzelRight = ovHiGR,
            WaveformOverviewLength = ovLen,
            WaveformOverviewSpanSeconds = source.WaveformOverviewSpanSeconds,
            WaveformOverviewValidSampleCount = source.WaveformOverviewValidSampleCount,
            WaveformOverviewNewestMonoSampleIndex = source.WaveformOverviewNewestMonoSampleIndex,
            WaveformOverviewOldestMonoSampleIndex = source.WaveformOverviewOldestMonoSampleIndex,
            WaveformOverviewBuiltValidSampleCount = source.WaveformOverviewBuiltValidSampleCount,
            WaveformOverviewBuiltNewestMonoSampleIndex = source.WaveformOverviewBuiltNewestMonoSampleIndex,
            WaveformOverviewBuiltOldestMonoSampleIndex = source.WaveformOverviewBuiltOldestMonoSampleIndex,
            WaveformBeatMarkMonoSampleIndex = bmS,
            WaveformBeatMarkBeatOrdinal = bmO,
            WaveformBeatMarkLength = bmLen,
            LeftChannel = source.LeftChannel,
            RightChannel = source.RightChannel,
            LeftPeakHold = source.LeftPeakHold,
            RightPeakHold = source.RightPeakHold
        };
    }

    private static float[] CopyFloatSegment(float[]? src, int length)
    {
        if (src is not { Length: > 0 } || length <= 0)
        {
            return Array.Empty<float>();
        }

        int n = Math.Min(length, src.Length);
        var dst = GC.AllocateUninitializedArray<float>(n);
        Array.Copy(src, dst, n);
        return dst;
    }

    private static long[] CopyLongSegment(long[]? src, int length)
    {
        if (src is not { Length: > 0 } || length <= 0)
        {
            return Array.Empty<long>();
        }

        int n = Math.Min(length, src.Length);
        var dst = GC.AllocateUninitializedArray<long>(n);
        Array.Copy(src, dst, n);
        return dst;
    }

    private static int[] CopyIntSegment(int[]? src, int length)
    {
        if (src is not { Length: > 0 } || length <= 0)
        {
            return Array.Empty<int>();
        }

        int n = Math.Min(length, src.Length);
        var dst = GC.AllocateUninitializedArray<int>(n);
        Array.Copy(src, dst, n);
        return dst;
    }

    private void ApplyWindow()
    {
        for (int i = 0; i < FftLength; i++)
        {
            float window = 0.54f - 0.46f * MathF.Cos(2 * MathF.PI * i / (FftLength - 1));
            _fftBuffer[i].X *= window;
        }
    }
}
