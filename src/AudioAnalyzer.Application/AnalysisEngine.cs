using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.BeatDetection;
using AudioAnalyzer.Application.Fft;
using AudioAnalyzer.Application.VolumeAnalysis;

namespace AudioAnalyzer.Application;

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
    private int _lastTerminalWidth;
    private int _lastTerminalHeight;
    private int _headerStartRow = 6;
    private int _displayStartRow = 6;
    private bool _fullScreen;
    private int _overlayRowCount;
    private Action? _onRedrawHeader;
    private Action? _onRefreshHeader;
    private Func<bool>? _renderGuard;

    private int _numBands;
    private double _instantEnergy;

    private readonly IVisualizationRenderer _renderer;
    private readonly IBeatDetector _beatDetector;
    private readonly IVolumeAnalyzer _volumeAnalyzer;
    private readonly IFftBandProcessor _fftBandProcessor;
    private readonly IDisplayDimensions _displayDimensions;
    private readonly AnalysisSnapshot _snapshot = new();
    private readonly object _renderLock = new();
    private object? _consoleLock;

    public AnalysisEngine(IVisualizationRenderer renderer, IDisplayDimensions displayDimensions, IBeatDetector beatDetector, IVolumeAnalyzer volumeAnalyzer, IFftBandProcessor fftBandProcessor)
    {
        _renderer = renderer;
        _displayDimensions = displayDimensions;
        _beatDetector = beatDetector ?? throw new ArgumentNullException(nameof(beatDetector));
        _volumeAnalyzer = volumeAnalyzer ?? throw new ArgumentNullException(nameof(volumeAnalyzer));
        _fftBandProcessor = fftBandProcessor ?? throw new ArgumentNullException(nameof(fftBandProcessor));
        UpdateDisplayDimensions();
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

    /// <summary>When true, the visualizer uses the full console; header and toolbar are hidden.</summary>
    public bool FullScreen
    {
        get => _fullScreen;
        set
        {
            _fullScreen = value;
            UpdateDisplayStartRow();
        }
    }

    /// <summary>
    /// When overlay is active, the engine uses overlayRowCount as DisplayStartRow and skips the header refresh,
    /// so an overlay can occupy the top rows while the visualizer keeps running below. Call with active=false to restore.
    /// </summary>
    public void SetOverlayActive(bool active, int overlayRowCount = 0)
    {
        _overlayRowCount = active ? overlayRowCount : 0;
        UpdateDisplayStartRow();
    }

    private void UpdateDisplayStartRow()
    {
        if (_overlayRowCount > 0)
        {
            _displayStartRow = _overlayRowCount;
        }
        else if (_fullScreen)
        {
            _displayStartRow = 0;
        }
        else
        {
            _displayStartRow = _headerStartRow;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="redrawHeader">Full redraw (clear + header), e.g. on resize or keypress.</param>
    /// <param name="refreshHeader">Optional: redraw only the header lines (no clear), called before each render so the top never disappears.</param>
    public void SetHeaderCallback(Action? redrawHeader, Action? refreshHeader, int startRow)
    {
        _onRedrawHeader = redrawHeader;
        _onRefreshHeader = refreshHeader;
        _headerStartRow = startRow;
        UpdateDisplayStartRow();
    }

    /// <summary>
    /// When set, the engine skips rendering (ProcessAudio and Redraw) when the guard returns false (e.g. when a modal is open).
    /// </summary>
    public void SetRenderGuard(Func<bool>? guard)
    {
        _renderGuard = guard;
    }

    /// <summary>
    /// Optional lock for console output. When set, the engine acquires it before header refresh and render,
    /// so overlays and other console writers can serialize their output and avoid interleaved corruption.
    /// </summary>
    public void SetConsoleLock(object? consoleLock)
    {
        _consoleLock = consoleLock;
    }

    /// <summary>
    /// Refreshes only the header (device, now-playing, BPM, volume) without re-rendering the visualizer.
    /// Called by a periodic UI timer so scrolling text and header elements animate even when no audio
    /// is playing (e.g. WASAPI loopback does not fire DataAvailable when silent).
    /// </summary>
    public void RefreshHeaderIfNeeded()
    {
        if (_renderGuard != null && !_renderGuard())
        {
            return;
        }
        if (_fullScreen || _overlayRowCount > 0)
        {
            return;
        }
        int w = _displayDimensions.Width;
        int h = _displayDimensions.Height;
        if (w < 30 || h < 15)
        {
            return;
        }

        void DoRefreshHeader()
        {
            _onRefreshHeader?.Invoke();
        }

        if (_consoleLock != null)
        {
            lock (_consoleLock)
            {
                lock (_renderLock)
                {
                    DoRefreshHeader();
                }
            }
        }
        else
        {
            lock (_renderLock)
            {
                DoRefreshHeader();
            }
        }
    }

    /// <summary>
    /// Redraw the toolbar and visualizer once using current dimensions and last snapshot data.
    /// Call after DrawMainUI so the toolbar appears immediately instead of waiting for the next audio-driven frame.
    /// </summary>
    public void Redraw()
    {
        RedrawInternal(useFullHeaderRedraw: false);
    }

    /// <summary>
    /// Performs a full redraw: clears console, draws header, then toolbar and visualizer. All under the console lock
    /// to prevent race with the header refresh timer. Use after preset/device change or other actions that require
    /// the title bar (e.g. preset name) to be updated.
    /// </summary>
    public void RedrawWithFullHeader()
    {
        RedrawInternal(useFullHeaderRedraw: true);
    }

    private void RedrawInternal(bool useFullHeaderRedraw)
    {
        if (_renderGuard != null && !_renderGuard())
        {
            return;
        }

        void DoRender()
        {
            int w = _displayDimensions.Width;
            int h = _displayDimensions.Height;
            if (w < 30 || h < 15)
            {
                return;
            }

            _snapshot.FullScreenMode = _fullScreen;
            _snapshot.DisplayStartRow = _displayStartRow;
            _snapshot.TerminalWidth = w;
            _snapshot.TerminalHeight = h;
            if (!_fullScreen && _overlayRowCount == 0)
            {
                if (useFullHeaderRedraw)
                {
                    _onRedrawHeader?.Invoke();
                }
                else
                {
                    _onRefreshHeader?.Invoke();
                }
            }
            try { _renderer.Render(_snapshot); } catch (Exception ex) { _ = ex; /* Render failed: swallow to avoid crash */ }
        }

        if (_consoleLock != null)
        {
            lock (_consoleLock)
            {
                lock (_renderLock)
                {
                    DoRender();
                }
            }
        }
        else
        {
            lock (_renderLock)
            {
                DoRender();
            }
        }
    }

    public void ProcessAudio(byte[] buffer, int bytesRecorded, AudioFormat format)
    {
        int bytesPerFrame = format.BytesPerFrame;
        int framesRecorded = bytesRecorded / bytesPerFrame;
        int bytesPerSample = format.BytesPerSample;
        int channels = format.Channels;

        float maxVolume = 0;
        float maxLeft = 0, maxRight = 0;

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
            _instantEnergy += mono * mono;

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

        double avgEnergy = framesRecorded > 0 ? Math.Sqrt(_instantEnergy / framesRecorded) : 0;
        _beatDetector.ProcessFrame(avgEnergy);
        _instantEnergy = 0;

        if ((DateTime.Now - _lastUpdate).TotalMilliseconds >= UpdateIntervalMs)
        {
            Array.Copy(_waveformBuffer, _displayWaveform, WaveformSize);
            _displayWaveformPosition = _waveformPosition;
            int w = _displayDimensions.Width;
            int h = _displayDimensions.Height;
            if (w >= 30 && h >= 15 && (_renderGuard == null || _renderGuard()))
            {
                if (w != _lastTerminalWidth || h != _lastTerminalHeight)
                {
                    UpdateDisplayDimensions();
                    if (!_fullScreen && _overlayRowCount == 0)
                    {
                        _onRedrawHeader?.Invoke();
                    }
                }

                void DoProcessAudioRender()
                {
                    if (!_fullScreen && _overlayRowCount == 0)
                    {
                        _onRefreshHeader?.Invoke();
                    }
                    FillSnapshot(w, h);
                    try { _renderer.Render(_snapshot); } catch (Exception ex) { _ = ex; /* Render failed: swallow to avoid crash */ }
                }

                if (_consoleLock != null)
                {
                    lock (_consoleLock)
                    {
                        lock (_renderLock)
                        {
                            DoProcessAudioRender();
                        }
                    }
                }
                else
                {
                    lock (_renderLock)
                    {
                        DoProcessAudioRender();
                    }
                }
            }
            _lastUpdate = DateTime.Now;
            _beatDetector.DecayFlashFrame();
        }
    }

    private void FillSnapshot(int termWidth, int termHeight)
    {
        _snapshot.FullScreenMode = _fullScreen;
        _snapshot.DisplayStartRow = _displayStartRow;
        _snapshot.TerminalWidth = termWidth;
        _snapshot.TerminalHeight = termHeight;
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

    private void UpdateDisplayDimensions()
    {
        int w = _displayDimensions.Width;
        int h = _displayDimensions.Height;
        if (w < 30 || h < 15)
        {
            if (_numBands == 0)
            {
                _numBands = 8;
            }

            return;
        }
        _numBands = Math.Max(8, Math.Min(60, (w - 8) / 2));
        _lastTerminalWidth = w;
        _lastTerminalHeight = h;
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
