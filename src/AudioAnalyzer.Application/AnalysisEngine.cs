using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.BeatDetection;
using AudioAnalyzer.Application.Fft;
using AudioAnalyzer.Application.VolumeAnalysis;

namespace AudioAnalyzer.Application;

/// <summary>
/// Performs analysis on incoming audio and exposes results via properties and GetSnapshot().
/// Callers/orchestrators are responsible for display dimensions, rendering, and header refresh.
/// </summary>
public sealed class AnalysisEngine
{
    private const int FftLength = 8192;
    private const int WaveformSize = 512;
    private const int UpdateIntervalMs = 50;
    private static readonly int FftLog2N = (int)Math.Log2(FftLength);

    private readonly ComplexFloat[] _fftBuffer = new ComplexFloat[FftLength];
    private int _bufferPosition;
    private readonly float[] _waveformBuffer = new float[WaveformSize];
    private int _waveformPosition;
    private readonly float[] _displayWaveform = new float[WaveformSize];
    private int _displayWaveformPosition;

    private DateTime _lastUpdate = DateTime.Now;
    private int _numBands = 8;

    private readonly IBeatDetector _beatDetector;
    private readonly IVolumeAnalyzer _volumeAnalyzer;
    private readonly IFftBandProcessor _fftBandProcessor;
    private readonly AnalysisSnapshot _snapshot = new();

    public AnalysisEngine(IBeatDetector beatDetector, IVolumeAnalyzer volumeAnalyzer, IFftBandProcessor fftBandProcessor)
    {
        _beatDetector = beatDetector ?? throw new ArgumentNullException(nameof(beatDetector));
        _volumeAnalyzer = volumeAnalyzer ?? throw new ArgumentNullException(nameof(volumeAnalyzer));
        _fftBandProcessor = fftBandProcessor ?? throw new ArgumentNullException(nameof(fftBandProcessor));
    }

    /// <summary>Beat detection sensitivity (0.5–3.0). Delegates to IBeatDetector.</summary>
    public double BeatSensitivity { get => _beatDetector.BeatSensitivity; set => _beatDetector.BeatSensitivity = value; }

    /// <summary>Current detected BPM from beat detection. 0 when no detection yet.</summary>
    public double CurrentBpm => _beatDetector.CurrentBpm;

    /// <summary>True when a beat was recently detected (used for visual flash effects).</summary>
    public bool BeatFlashActive => _beatDetector.BeatFlashActive;

    /// <summary>Latest volume from audio processing (0–1). Used for header display.</summary>
    public float Volume => _volumeAnalyzer.Volume;

    /// <summary>Incremented each time a beat is detected. Used for Show playback with beats duration.</summary>
    public int BeatCount => _beatDetector.BeatCount;

    /// <summary>
    /// Sets the number of FFT bands to compute. Call from the orchestrator when display dimensions change.
    /// </summary>
    public void SetNumBands(int numBands)
    {
        _numBands = Math.Max(8, Math.Min(60, numBands));
    }

    /// <summary>
    /// Returns the current analysis snapshot (analysis data only). Display fields (DisplayStartRow,
    /// TerminalWidth, TerminalHeight, FullScreenMode) are left at default; the caller/orchestrator sets them before rendering.
    /// </summary>
    public AnalysisSnapshot GetSnapshot()
    {
        FillSnapshot();
        return _snapshot;
    }

    /// <summary>
    /// Processes incoming audio: volume, waveform, FFT bands, beat detection. Updates internal state and
    /// the display waveform at UpdateIntervalMs. Does not perform any rendering or header refresh.
    /// </summary>
    public void ProcessAudio(byte[] buffer, int bytesRecorded, AudioFormat format)
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
        _beatDetector.ProcessFrame(avgEnergy);

        if ((DateTime.Now - _lastUpdate).TotalMilliseconds >= UpdateIntervalMs)
        {
            Array.Copy(_waveformBuffer, _displayWaveform, WaveformSize);
            _displayWaveformPosition = _waveformPosition;
            _lastUpdate = DateTime.Now;
            _beatDetector.DecayFlashFrame();
        }
    }

    private void FillSnapshot()
    {
        _snapshot.Volume = _volumeAnalyzer.Volume;
        _snapshot.CurrentBpm = _beatDetector.CurrentBpm;
        _snapshot.BeatSensitivity = _beatDetector.BeatSensitivity;
        _snapshot.BeatFlashActive = _beatDetector.BeatFlashActive;
        _snapshot.BeatCount = _beatDetector.BeatCount;
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

    private void ApplyWindow()
    {
        for (int i = 0; i < FftLength; i++)
        {
            float window = 0.54f - 0.46f * MathF.Cos(2 * MathF.PI * i / (FftLength - 1));
            _fftBuffer[i].X *= window;
        }
    }
}
