using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using Microsoft.Extensions.Logging;

namespace AudioAnalyzer.Console;

/// <summary>Header region: title bar; in Preset/Show modes also device/now and BPM/beat/volume rows. In General settings, only the title breadcrumb row. Implements <see cref="IUiComponent"/> and composes child <see cref="IUiComponent"/>s; renders via <see cref="IUiComponentRenderer{TComponent}"/>.</summary>
internal sealed partial class HeaderContainer : IHeaderContainer, IUiComponent
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
    private readonly IDisplayFrameClock _displayFrameClock;
    private readonly ILogger<HeaderContainer> _logger;

    private readonly HorizontalRowComponent _titleRow;
    private readonly HorizontalRowComponent _row2;
    private readonly HorizontalRowComponent _row3;

    private string _deviceName = "";
    private string? _nowPlayingText;
    private double _currentBpm = -1;
    private float _volume = -1;
    private string _bpmCellValue = "";
    private string _beatCellValue = "";
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
        IApplicationModeFactory applicationModeFactory,
        IDisplayFrameClock displayFrameClock,
        ILogger<HeaderContainer> logger)
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
        _displayFrameClock = displayFrameClock ?? throw new ArgumentNullException(nameof(displayFrameClock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
            LogConsoleBufferResizeFailed(ex);
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
        _volume = data.Volume;
        _bpmCellValue = data.BpmCellValue ?? "";
        _beatCellValue = data.BeatCellValue ?? "";
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
            InvalidateWriteCache = invalidateCache,
            FrameDeltaSeconds = _displayFrameClock.FrameDeltaSeconds
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
        LabeledValueDescriptor bpmDescriptor = _currentBpm >= 0
            ? new LabeledValueDescriptor("BPM", () => new PlainText(_bpmCellValue))
            : new LabeledValueDescriptor("", () => new PlainText(""));
        LabeledValueDescriptor beatDescriptor = _currentBpm >= 0
            ? new LabeledValueDescriptor("Beat", () => new PlainText(_beatCellValue))
            : new LabeledValueDescriptor("", () => new PlainText(""));
        LabeledValueDescriptor volumeDescriptor = _volume >= 0
            ? new LabeledValueDescriptor("Volume/dB", () => new PlainText(_volumeText))
            : new LabeledValueDescriptor("", () => new PlainText(""));
        return [bpmDescriptor, beatDescriptor, volumeDescriptor];
    }

    private static IReadOnlyList<int> BuildRow3Widths(int width)
    {
        int a = Math.Max(8, (width / 3 / 8) * 8);
        int b = Math.Max(8, ((width - a) / 2 / 8) * 8);
        int c = width - a - b;
        if (c < 8 && width >= 24)
        {
            b = Math.Max(8, width - a - 8);
            c = width - a - b;
        }
        return [a, b, Math.Max(1, c)];
    }

    [LoggerMessage(EventId = 7610, Level = LogLevel.Warning, Message = "Console buffer resize not supported or failed")]
    private partial void LogConsoleBufferResizeFailed(Exception ex);
}
