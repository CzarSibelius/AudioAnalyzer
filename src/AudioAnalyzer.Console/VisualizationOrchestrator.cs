using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Console;

/// <summary>
/// Orchestrates display and rendering: holds display state, header callbacks, render guard and console lock,
/// and drives header refresh and visualizer render using analysis results from the analysis engine.
/// </summary>
/// <remarks>
/// <para><strong>Responsibility.</strong> Orchestrator owns the display pipeline: overlay, header row, when to refresh the header
/// and when to run one frame (throttling, guard, dimensions), and execution of one frame
/// (header callback + engine snapshot + renderer). It receives audio via <see cref="IVisualizationOrchestrator.OnAudioData"/>
/// and drives throttled render from there. <see cref="ApplicationShell"/> configures this orchestrator and triggers
/// Redraw/RedrawWithFullHeader in response to user or app events; the orchestrator does not decide app logic.</para>
/// </remarks>
internal sealed class VisualizationOrchestrator : IVisualizationOrchestrator
{
    private const int UpdateIntervalMs = 50;
    private const int MinWidth = 30;
    private const int MinHeight = 15;

    private DateTime _lastUpdate = DateTime.Now;
    private int _lastTerminalWidth;
    private int _lastTerminalHeight;
    private int _headerStartRow = 6;
    private int _displayStartRow = 6;
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

    public VisualizationOrchestrator(
        AnalysisEngine engine,
        IVisualizationRenderer renderer,
        IDisplayDimensions displayDimensions,
        IDisplayState displayState)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _displayDimensions = displayDimensions ?? throw new ArgumentNullException(nameof(displayDimensions));
        _displayState = displayState ?? throw new ArgumentNullException(nameof(displayState));
        _displayState.Changed += (_, _) => UpdateDisplayStartRow();
        UpdateNumBandsFromDimensions();
    }

    /// <inheritdoc />
    public void SetHeaderCallback(Action? redrawHeader, Action? refreshHeader, int startRow)
    {
        _onRedrawHeader = redrawHeader;
        _onRefreshHeader = refreshHeader;
        _headerStartRow = startRow;
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
    public void OnAudioData(byte[] buffer, int bytesRecorded, AudioFormat format)
    {
        _engine.ProcessAudio(buffer, bytesRecorded, format);

        if ((DateTime.Now - _lastUpdate).TotalMilliseconds < UpdateIntervalMs)
        {
            return;
        }

        _lastUpdate = DateTime.Now;

        int w = _displayDimensions.Width;
        int h = _displayDimensions.Height;
        if (w < MinWidth || h < MinHeight || (_renderGuard != null && !_renderGuard()))
        {
            return;
        }

        if (w != _lastTerminalWidth || h != _lastTerminalHeight)
        {
            UpdateNumBandsFromDimensions();
            _lastTerminalWidth = w;
            _lastTerminalHeight = h;
            if (!_displayState.FullScreen && _overlayRowCount == 0)
            {
                _onRedrawHeader?.Invoke();
            }
        }

        DoRender(w, h, useFullHeaderRedraw: false);
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

        DoRender(w, h, useFullHeaderRedraw);
    }

    private void DoRender(int w, int h, bool useFullHeaderRedraw)
    {
        void RenderCore()
        {
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

            try
            {
                _renderer.Render(snapshot);
            }
            catch (Exception ex)
            {
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
            _displayStartRow = _headerStartRow;
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
