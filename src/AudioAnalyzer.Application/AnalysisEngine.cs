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
/// and per-call snapshot copies keep those paths thread-safe.
/// </remarks>
public sealed class AnalysisEngine
{
    private const int FftLength = 8192;
    private const int WaveformSize = 512;
    private const int UpdateIntervalMs = 50;
    private static readonly int FftLog2N = (int)Math.Log2(FftLength);

    private readonly object _sync = new();

    private readonly ComplexFloat[] _fftBuffer = new ComplexFloat[FftLength];
    private int _bufferPosition;
    private readonly float[] _waveformBuffer = new float[WaveformSize];
    private int _waveformPosition;
    private readonly float[] _displayWaveform = new float[WaveformSize];
    private int _displayWaveformPosition;

    private DateTime _lastUpdate = DateTime.Now;
    private DateTime _lastBeatVisualPulse = DateTime.MinValue;
    private int _numBands = 8;

    private readonly IBeatTimingSource _beatTiming;
    private readonly IVolumeAnalyzer _volumeAnalyzer;
    private readonly IFftBandProcessor _fftBandProcessor;
    private readonly AnalysisSnapshot _snapshot = new();

    public AnalysisEngine(IBeatTimingSource beatTiming, IVolumeAnalyzer volumeAnalyzer, IFftBandProcessor fftBandProcessor)
    {
        _beatTiming = beatTiming ?? throw new ArgumentNullException(nameof(beatTiming));
        _volumeAnalyzer = volumeAnalyzer ?? throw new ArgumentNullException(nameof(volumeAnalyzer));
        _fftBandProcessor = fftBandProcessor ?? throw new ArgumentNullException(nameof(fftBandProcessor));
    }

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
    /// Returns a copy of the current analysis snapshot (analysis data only). Array properties are cloned so callers can
    /// render without racing <see cref="ProcessAudio"/>. Display fields (DisplayStartRow, TerminalWidth, TerminalHeight,
    /// MeasuredMainRenderFps, FrameDeltaSeconds) are defaults unless the caller sets them before rendering.
    /// </summary>
    public AnalysisSnapshot GetSnapshot()
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

            _waveformBuffer[_waveformPosition] = mono;
            _waveformPosition = (_waveformPosition + 1) % WaveformSize;

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

        if ((DateTime.Now - _lastUpdate).TotalMilliseconds >= UpdateIntervalMs)
        {
            Array.Copy(_waveformBuffer, _displayWaveform, WaveformSize);
            _displayWaveformPosition = _waveformPosition;
            _lastUpdate = DateTime.Now;
            PulseBeatVisualIfDueCore();
        }
    }

    private void PulseBeatVisualIfDueCore()
    {
        DateTime now = DateTime.Now;
        if ((now - _lastBeatVisualPulse).TotalMilliseconds < UpdateIntervalMs)
        {
            return;
        }

        _lastBeatVisualPulse = now;
        _beatTiming.OnVisualTick();
    }

    private void FillSnapshot()
    {
        _snapshot.Volume = _volumeAnalyzer.Volume;
        _snapshot.CurrentBpm = _beatTiming.CurrentBpm;
        _snapshot.BeatSensitivity = _beatTiming.BeatSensitivity;
        _snapshot.BeatFlashActive = _beatTiming.BeatFlashActive;
        _snapshot.BeatCount = _beatTiming.BeatCount;
        _snapshot.NumBands = _fftBandProcessor.NumBands;
        _snapshot.SmoothedMagnitudes = _fftBandProcessor.SmoothedMagnitudes;
        _snapshot.PeakHold = _fftBandProcessor.PeakHold;
        _snapshot.TargetMaxMagnitude = _fftBandProcessor.TargetMaxMagnitude;
        _snapshot.Waveform = _displayWaveform;
        _snapshot.WaveformPosition = _displayWaveformPosition;
        _snapshot.WaveformSize = WaveformSize;
        _snapshot.LeftChannel = _volumeAnalyzer.LeftChannel;
        _snapshot.RightChannel = _volumeAnalyzer.RightChannel;
        _snapshot.LeftPeakHold = _volumeAnalyzer.LeftPeakHold;
        _snapshot.RightPeakHold = _volumeAnalyzer.RightPeakHold;
    }

    private static AnalysisSnapshot CloneSnapshotArrays(AnalysisSnapshot source)
    {
        double[] sm = source.SmoothedMagnitudes ?? Array.Empty<double>();
        double[] ph = source.PeakHold ?? Array.Empty<double>();
        float[] wf = source.Waveform ?? Array.Empty<float>();

        var smCopy = sm.Length == 0 ? Array.Empty<double>() : new double[sm.Length];
        if (sm.Length > 0)
        {
            Array.Copy(sm, smCopy, sm.Length);
        }

        var phCopy = ph.Length == 0 ? Array.Empty<double>() : new double[ph.Length];
        if (ph.Length > 0)
        {
            Array.Copy(ph, phCopy, ph.Length);
        }

        var wfCopy = wf.Length == 0 ? Array.Empty<float>() : new float[wf.Length];
        if (wf.Length > 0)
        {
            Array.Copy(wf, wfCopy, wf.Length);
        }

        return new AnalysisSnapshot
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
            LeftChannel = source.LeftChannel,
            RightChannel = source.RightChannel,
            LeftPeakHold = source.LeftPeakHold,
            RightPeakHold = source.RightPeakHold
        };
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
