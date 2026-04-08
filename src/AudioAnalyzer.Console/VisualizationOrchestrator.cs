using System.Diagnostics;
using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Orchestrates display and rendering: holds display state, header callbacks, render guard and console lock,
/// and drives header refresh and visualizer render using analysis results from the analysis engine.
/// </summary>
/// <remarks>
/// <para><strong>Responsibility.</strong> Orchestrator owns the display pipeline: overlay, header row, when to refresh the header
/// and when to run one frame (guard, dimensions), and execution of one frame
/// (header callback + engine snapshot + renderer). <see cref="IVisualizationOrchestrator.OnAudioData"/> feeds analysis only;
/// <see cref="ApplicationShell"/> drives display cadence by calling <see cref="Redraw"/> / <see cref="RedrawWithFullHeader"/>
/// each main-loop tick and on user events. The orchestrator does not decide app logic.</para>
/// </remarks>
internal sealed class VisualizationOrchestrator : IVisualizationOrchestrator
{
    private const int MinWidth = 30;
    private const int MinHeight = 15;

    private int _lastTerminalWidth;
    private int _lastTerminalHeight;
    private int _displayStartRow = 3;
    private int _overlayRowCount;
    private Action? _onRedrawHeader;
    private Action? _onRefreshHeader;
    private Func<bool>? _renderGuard;
    private object? _consoleLock;
    private readonly object _renderLock = new();

    private readonly AnalysisEngine _engine;
    private readonly IVisualizationRenderer _renderer;
    private readonly IDisplayDimensions _displayDimensions;
    private readonly IDisplayState _displayState;
    private readonly IApplicationModeHeaderProvider _headerLayout;
    private readonly UiSettings _uiSettings;
    private readonly MainRenderFpsMeter _fpsMeter;
    private readonly IDisplayFrameClock _displayFrameClock;
    private long _lastFrameTimestampTicks;
    private bool _hasFrameTimestamp;
    private double?[]? _cachedLayerRenderTimeMsForUi;

    public VisualizationOrchestrator(
        AnalysisEngine engine,
        IVisualizationRenderer renderer,
        IDisplayDimensions displayDimensions,
        IDisplayState displayState,
        IApplicationModeHeaderProvider headerLayout,
        UiSettings uiSettings,
        MainRenderFpsMeter fpsMeter,
        IDisplayFrameClock displayFrameClock)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _displayDimensions = displayDimensions ?? throw new ArgumentNullException(nameof(displayDimensions));
        _displayState = displayState ?? throw new ArgumentNullException(nameof(displayState));
        _headerLayout = headerLayout ?? throw new ArgumentNullException(nameof(headerLayout));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _fpsMeter = fpsMeter ?? throw new ArgumentNullException(nameof(fpsMeter));
        _displayFrameClock = displayFrameClock ?? throw new ArgumentNullException(nameof(displayFrameClock));
        _displayState.Changed += (_, _) => UpdateDisplayStartRow();
        UpdateNumBandsFromDimensions();
    }

    /// <inheritdoc />
    public void SetHeaderCallback(Action? redrawHeader, Action? refreshHeader)
    {
        _onRedrawHeader = redrawHeader;
        _onRefreshHeader = refreshHeader;
        UpdateDisplayStartRow();
    }

    /// <inheritdoc />
    public void SetRenderGuard(Func<bool>? guard)
    {
        _renderGuard = guard;
    }

    /// <inheritdoc />
    public void SetConsoleLock(object? consoleLock)
    {
        _consoleLock = consoleLock;
    }

    /// <inheritdoc />
    public void SetOverlayActive(bool active, int overlayRowCount = 0)
    {
        _overlayRowCount = active ? overlayRowCount : 0;
        UpdateDisplayStartRow();
    }

    /// <inheritdoc />
    public void RefreshHeaderIfNeeded()
    {
        if (_renderGuard != null && !_renderGuard())
        {
            return;
        }
        if (_displayState.FullScreen || _overlayRowCount > 0)
        {
            return;
        }
        int w = _displayDimensions.Width;
        int h = _displayDimensions.Height;
        if (w < MinWidth || h < MinHeight)
        {
            return;
        }

        void DoRefreshHeader()
        {
            _displayFrameClock.SetFrameDeltaSeconds(0.05);
            _engine.PulseBeatVisualIfDue();
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

    /// <inheritdoc />
    public void Redraw()
    {
        RedrawInternal(useFullHeaderRedraw: false);
    }

    /// <inheritdoc />
    public void RedrawWithFullHeader()
    {
        RedrawInternal(useFullHeaderRedraw: true);
    }

    /// <inheritdoc />
    public AnalysisSnapshot GetSnapshotForUi()
    {
        AnalysisSnapshot s = _engine.GetSnapshot();
        if (_uiSettings.ShowLayerRenderTime && _cachedLayerRenderTimeMsForUi != null)
        {
            s.LayerRenderTimeMs = CloneLayerRenderTimeMs(_cachedLayerRenderTimeMsForUi);
        }

        return s;
    }

    private static double?[]? CloneLayerRenderTimeMs(double?[]? src)
    {
        if (src == null)
        {
            return null;
        }

        var copy = new double?[src.Length];
        Array.Copy(src, copy, src.Length);
        return copy;
    }

    /// <inheritdoc />
    public void OnAudioData(byte[] buffer, int bytesRecorded, AudioFormat format)
    {
        _engine.ProcessAudio(buffer, bytesRecorded, format);
    }

    private void RedrawInternal(bool useFullHeaderRedraw)
    {
        if (_renderGuard != null && !_renderGuard())
        {
            return;
        }

        int w = _displayDimensions.Width;
        int h = _displayDimensions.Height;
        if (w < MinWidth || h < MinHeight)
        {
            return;
        }

        if (w != _lastTerminalWidth || h != _lastTerminalHeight)
        {
            UpdateNumBandsFromDimensions();
            if (!_displayState.FullScreen && _overlayRowCount == 0)
            {
                _onRedrawHeader?.Invoke();
            }
        }

        DoRender(w, h, useFullHeaderRedraw);
    }

    private void DoRender(int w, int h, bool useFullHeaderRedraw)
    {
        UpdateDisplayStartRow();

        void RenderCore()
        {
            long nowTicks = Stopwatch.GetTimestamp();
            double frameDeltaSeconds;
            if (!_hasFrameTimestamp)
            {
                frameDeltaSeconds = 1.0 / 60.0;
                _hasFrameTimestamp = true;
            }
            else
            {
                double elapsed = (nowTicks - _lastFrameTimestampTicks) / (double)Stopwatch.Frequency;
                frameDeltaSeconds = Math.Min(Math.Max(elapsed, 1.0 / 100_000.0), 0.25);
            }

            _lastFrameTimestampTicks = nowTicks;
            _displayFrameClock.SetFrameDeltaSeconds(frameDeltaSeconds);

            if (!_displayState.FullScreen && _overlayRowCount == 0)
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

            var snapshot = _engine.GetSnapshot();
            snapshot.DisplayStartRow = _displayStartRow;
            snapshot.TerminalWidth = w;
            snapshot.TerminalHeight = h;
            snapshot.FrameDeltaSeconds = frameDeltaSeconds;
            snapshot.MeasuredMainRenderFps = null;
            if (_uiSettings.ShowRenderFps && _fpsMeter.HasIntervalSample)
            {
                snapshot.MeasuredMainRenderFps = _fpsMeter.GetSmoothedFps();
            }

            try
            {
                _renderer.Render(snapshot);
                _fpsMeter.RecordFrameCompleted();
                if (_uiSettings.ShowLayerRenderTime && snapshot.LayerRenderTimeMs != null)
                {
                    _cachedLayerRenderTimeMsForUi = CloneLayerRenderTimeMs(snapshot.LayerRenderTimeMs);
                }
                else
                {
                    _cachedLayerRenderTimeMsForUi = null;
                }
            }
            catch (Exception ex)
            {
                _cachedLayerRenderTimeMsForUi = null;
                _ = ex; /* Render failed: swallow to avoid crash */
            }
        }

        if (_consoleLock != null)
        {
            lock (_consoleLock)
            {
                lock (_renderLock)
                {
                    RenderCore();
                }
            }
        }
        else
        {
            lock (_renderLock)
            {
                RenderCore();
            }
        }
    }

    private void UpdateDisplayStartRow()
    {
        if (_overlayRowCount > 0)
        {
            _displayStartRow = _overlayRowCount;
        }
        else if (_displayState.FullScreen)
        {
            _displayStartRow = 0;
        }
        else
        {
            _displayStartRow = _headerLayout.HeaderLineCount;
        }
    }

    private void UpdateNumBandsFromDimensions()
    {
        int w = _displayDimensions.Width;
        int h = _displayDimensions.Height;
        if (w < MinWidth || h < MinHeight)
        {
            return;
        }
        int numBands = Math.Max(8, Math.Min(60, (w - 8) / 2));
        _engine.SetNumBands(numBands);
        _lastTerminalWidth = w;
        _lastTerminalHeight = h;
    }
}
