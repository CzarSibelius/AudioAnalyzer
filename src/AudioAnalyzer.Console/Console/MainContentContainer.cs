using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Main content region: toolbar row + visualizer or settings hub. Implements <see cref="IVisualizationRenderer"/>
/// so the orchestrator and shell keep the same contract. Delegates layout to <see cref="IApplicationMode"/>.
/// </summary>
internal sealed class MainContentContainer : IVisualizationRenderer
{
    private const int ToolbarLineCount = 1;

    private readonly IVisualizer _visualizer;
    private readonly IDisplayState _displayState;
    private readonly UiSettings _uiSettings;
    private readonly IUiThemeResolver _uiThemeResolver;
    private readonly IUiComponentRenderer<IUiComponent> _componentRenderer;
    private readonly IUiStateUpdater<IUiComponent> _stateUpdater;
    private readonly HorizontalRowComponent _toolbarRow;
    private readonly ITextLayerBoundsEditSession _boundsEditSession;
    private readonly IApplicationModeFactory _applicationModeFactory;
    private readonly Lazy<IDeviceCaptureController> _deviceCapture;

    private (IReadOnlyList<PaletteColor>? Palette, string? DisplayName) _palette;

    public MainContentContainer(
        IUiComponentRenderer<IUiComponent> componentRenderer,
        IUiStateUpdater<IUiComponent> stateUpdater,
        IVisualizer visualizer,
        IDisplayState displayState,
        UiSettings uiSettings,
        IUiThemeResolver uiThemeResolver,
        ITextLayerBoundsEditSession boundsEditSession,
        IApplicationModeFactory applicationModeFactory,
        Lazy<IDeviceCaptureController> deviceCapture)
    {
        _componentRenderer = componentRenderer ?? throw new ArgumentNullException(nameof(componentRenderer));
        _stateUpdater = stateUpdater ?? throw new ArgumentNullException(nameof(stateUpdater));
        _visualizer = visualizer ?? throw new ArgumentNullException(nameof(visualizer));
        _displayState = displayState ?? throw new ArgumentNullException(nameof(displayState));
        _uiSettings = uiSettings ?? new UiSettings();
        _uiThemeResolver = uiThemeResolver ?? throw new ArgumentNullException(nameof(uiThemeResolver));
        _boundsEditSession = boundsEditSession ?? throw new ArgumentNullException(nameof(boundsEditSession));
        _applicationModeFactory = applicationModeFactory ?? throw new ArgumentNullException(nameof(applicationModeFactory));
        _deviceCapture = deviceCapture ?? throw new ArgumentNullException(nameof(deviceCapture));

        _displayState.Changed += (_, _) =>
        {
            if (!_displayState.FullScreen)
            {
                _componentRenderer.ResetVisualizerAreaCleared();
            }
        };

        _toolbarRow = new HorizontalRowComponent();
    }

    /// <inheritdoc />
    public void SetPalette(IReadOnlyList<PaletteColor>? palette, string? paletteDisplayName = null)
    {
        _palette = (palette, paletteDisplayName);
    }

    /// <inheritdoc />
    public bool SupportsPaletteCycling() =>
        _visualizer is { SupportsPaletteCycling: true };

    /// <inheritdoc />
    public bool HandleKey(ConsoleKeyInfo key)
    {
        var mode = _applicationModeFactory.GetActiveApplicationMode();
        if (mode.UsesGeneralSettingsHubKeyHandling)
        {
            return false;
        }

        return mode.TryHandleVisualizerKeys(key, _boundsEditSession, _visualizer);
    }

    /// <inheritdoc />
    public void Render(AnalysisSnapshot snapshot)
    {
        try
        {
            if (snapshot.TerminalWidth < 30 || snapshot.TerminalHeight < 15)
            {
                return;
            }

            int termWidth = snapshot.TerminalWidth;
            int startRow = snapshot.DisplayStartRow;
            var activeMode = _applicationModeFactory.GetActiveApplicationMode();
            bool layoutFullScreen = _displayState.FullScreen && activeMode.AllowsVisualizerFullscreen;

            if (layoutFullScreen)
            {
                startRow = 0;
            }
            else
            {
                if (startRow < 0 || startRow + ToolbarLineCount >= snapshot.TerminalHeight)
                {
                    return;
                }
            }

            int visualizerMaxLines = layoutFullScreen
                ? Math.Max(1, snapshot.TerminalHeight - 1)
                : Math.Max(1, snapshot.TerminalHeight - startRow - ToolbarLineCount - 1);

            UiPalette palette = _uiThemeResolver.GetEffectiveUiPalette();
            var context = new RenderContext
            {
                Width = termWidth,
                StartRow = startRow,
                MaxLines = visualizerMaxLines,
                Palette = palette,
                ScrollSpeed = _uiSettings.DefaultScrollingSpeed,
                Snapshot = snapshot,
                PaletteDisplayName = _palette.DisplayName,
                DeviceName = _deviceCapture.Value.CurrentDeviceName
            };

            var root = new CompositeComponent(ctx => activeMode.GetMainComponents(new MainContentRenderArgs
            {
                Context = ctx,
                ToolbarRow = _toolbarRow,
                DisplayState = _displayState,
                Visualizer = _visualizer,
                UiSettings = _uiSettings,
                EffectiveUiPalette = palette,
                PaletteForSwatch = _palette.Palette,
                PaletteDisplayName = _palette.DisplayName
            }));
            _stateUpdater.Update(root, context);
            _componentRenderer.Render(root, context);
        }
        catch (Exception ex)
        {
            _ = ex; /* Last-resort render failure: swallow to avoid crash */
        }
    }
}
