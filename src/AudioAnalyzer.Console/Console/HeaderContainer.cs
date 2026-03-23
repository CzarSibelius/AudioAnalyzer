using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Header region: title bar; in Preset/Show modes also device/now and BPM/volume rows. In General settings, only the title breadcrumb row. Implements <see cref="IUiComponent"/> and composes child <see cref="IUiComponent"/>s; renders via <see cref="IUiComponentRenderer{TComponent}"/>.</summary>
internal sealed class HeaderContainer : IHeaderContainer, IUiComponent
{
    private readonly IUiComponentRenderer<IUiComponent> _componentRenderer;
    private readonly IUiStateUpdater<IUiComponent> _stateUpdater;
    private readonly IDisplayDimensions _displayDimensions;
    private readonly INowPlayingProvider _nowPlayingProvider;
    private readonly AnalysisEngine _engine;
    private readonly UiSettings _uiSettings;
    private readonly IUiThemeResolver _uiThemeResolver;
    private readonly ITitleBarContentProvider _titleBarContentProvider;
    private readonly IApplicationModeFactory _applicationModeFactory;

    private readonly HorizontalRowComponent _titleRow;
    private readonly HorizontalRowComponent _row2;
    private readonly HorizontalRowComponent _row3;

    private string _deviceName = "";
    private string? _nowPlayingText;
    private double _currentBpm = -1;
    private double _beatSensitivity = 1.3;
    private bool _beatFlashActive;
    private float _volume = -1;
    private string _bpmBeatValue = "";
    private string _volumeText = "";

    public HeaderContainer(
        IUiComponentRenderer<IUiComponent> componentRenderer,
        IUiStateUpdater<IUiComponent> stateUpdater,
        IDisplayDimensions displayDimensions,
        INowPlayingProvider nowPlayingProvider,
        AnalysisEngine engine,
        UiSettings uiSettings,
        IUiThemeResolver uiThemeResolver,
        ITitleBarContentProvider titleBarContentProvider,
        IApplicationModeFactory applicationModeFactory)
    {
        _componentRenderer = componentRenderer ?? throw new ArgumentNullException(nameof(componentRenderer));
        _stateUpdater = stateUpdater ?? throw new ArgumentNullException(nameof(stateUpdater));
        _displayDimensions = displayDimensions ?? throw new ArgumentNullException(nameof(displayDimensions));
        _nowPlayingProvider = nowPlayingProvider ?? throw new ArgumentNullException(nameof(nowPlayingProvider));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _uiThemeResolver = uiThemeResolver ?? throw new ArgumentNullException(nameof(uiThemeResolver));
        _titleBarContentProvider = titleBarContentProvider ?? throw new ArgumentNullException(nameof(titleBarContentProvider));
        _applicationModeFactory = applicationModeFactory ?? throw new ArgumentNullException(nameof(applicationModeFactory));

        _titleRow = new HorizontalRowComponent();
        _row2 = new HorizontalRowComponent();
        _row3 = new HorizontalRowComponent();
    }

    /// <inheritdoc />
    public void DrawMain(string deviceName)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                int w = _displayDimensions.Width;
                int h = Math.Max(15, _displayDimensions.Height);
                if (w >= 10 && h >= 15)
                {
                    System.Console.BufferWidth = w;
                    System.Console.BufferHeight = h;
                }
            }
        }
        catch (Exception ex)
        {
            _ = ex; /* Buffer size not supported: swallow to avoid crash */
        }

        System.Console.Clear();
        System.Console.CursorVisible = false;

        var context = BuildContext(deviceName, invalidateCache: true);
        _stateUpdater.Update(this, context);
        _componentRenderer.Render(this, context);
    }

    /// <inheritdoc />
    public void DrawHeaderOnly(string deviceName)
    {
        var context = BuildContext(deviceName, invalidateCache: false);
        _stateUpdater.Update(this, context);
        _componentRenderer.Render(this, context);
    }

    /// <summary>Applies state from the state updater. Called by <see cref="HeaderContainerStateUpdater"/> before render.</summary>
    internal void ApplyState(HeaderStateData data)
    {
        _deviceName = data.DeviceName ?? "";
        _nowPlayingText = data.NowPlayingText;
        _currentBpm = data.CurrentBpm;
        _beatSensitivity = data.BeatSensitivity;
        _beatFlashActive = data.BeatFlashActive;
        _volume = data.Volume;
        _bpmBeatValue = data.BpmBeatValue ?? "";
        _volumeText = data.VolumeText ?? "";
    }

    /// <inheritdoc />
    public IReadOnlyList<IUiComponent>? GetChildren(RenderContext context)
    {
        var titleDescriptor = new LabeledValueDescriptor("", () => _titleBarContentProvider.GetTitleBarContent(), preformattedAnsi: true);
        _titleRow.SetRowData([titleDescriptor], [context.Width]);
        int headerLines = _applicationModeFactory.GetActiveApplicationMode().HeaderLineCount;
        if (headerLines <= 1)
        {
            return [_titleRow];
        }

        _row2.SetRowData(BuildRow2Viewports(), BuildRow2Widths(context.Width));
        _row3.SetRowData(BuildRow3Viewports(), BuildRow3Widths(context.Width));
        return [_titleRow, _row2, _row3];
    }

    private RenderContext BuildContext(string deviceName, bool invalidateCache)
    {
        int width = Math.Max(10, _displayDimensions.Width);
        UiPalette palette = _uiThemeResolver.GetEffectiveUiPalette();
        int headerLines = _applicationModeFactory.GetActiveApplicationMode().HeaderLineCount;
        return new RenderContext
        {
            Width = width,
            StartRow = 0,
            MaxLines = Math.Max(1, headerLines),
            Palette = palette,
            ScrollSpeed = _uiSettings.DefaultScrollingSpeed,
            DeviceName = deviceName,
            InvalidateWriteCache = invalidateCache
        };
    }

    private IReadOnlyList<LabeledValueDescriptor> BuildRow2Viewports()
    {
        var palette = _uiThemeResolver.GetEffectiveUiPalette();
        var nowTextColor = !string.IsNullOrEmpty(_nowPlayingText) ? palette.Highlighted : (PaletteColor?)null;
        return
        [
            new LabeledValueDescriptor("Device", () => new PlainText(_deviceName)),
            new LabeledValueDescriptor("Now", () => new PlainText(_nowPlayingText ?? ""), textColor: nowTextColor)
        ];
    }

    private static IReadOnlyList<int> BuildRow2Widths(int width)
    {
        int leftCellWidth = Math.Max(16, (width / 2 / 8) * 8);
        int rightCellWidth = width - leftCellWidth;
        return [leftCellWidth, rightCellWidth];
    }

    private IReadOnlyList<LabeledValueDescriptor> BuildRow3Viewports()
    {
        string bpmLabel = _currentBpm >= 0 ? (_currentBpm > 0 ? "BPM" : "Beat") : "";
        return
        [
            _currentBpm >= 0
                ? new LabeledValueDescriptor(bpmLabel, () => new PlainText(_bpmBeatValue))
                : new LabeledValueDescriptor("", () => new PlainText("")),
            _volume >= 0
                ? new LabeledValueDescriptor("Volume/dB", () => new PlainText(_volumeText))
                : new LabeledValueDescriptor("", () => new PlainText(""))
        ];
    }

    private static IReadOnlyList<int> BuildRow3Widths(int width)
    {
        int bpmCellWidth = Math.Max(8, (width / 2 / 8) * 8);
        int volCellWidth = width - bpmCellWidth;
        return [bpmCellWidth, volCellWidth];
    }
}
