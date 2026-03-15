using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Header region: title bar, device/now row, BPM/volume row. Implements <see cref="IUiComponent"/> and composes child <see cref="IUiComponent"/>s; renders via <see cref="IUiComponentRenderer{TComponent}"/>.</summary>
internal sealed class HeaderContainer : IHeaderContainer, IUiComponent
{
    private readonly IUiComponentRenderer<IUiComponent> _componentRenderer;
    private readonly IUiStateUpdater<IUiComponent> _stateUpdater;
    private readonly IDisplayDimensions _displayDimensions;
    private readonly INowPlayingProvider _nowPlayingProvider;
    private readonly AnalysisEngine _engine;
    private readonly UiSettings _uiSettings;
    private readonly ITitleBarContentProvider _titleBarContentProvider;

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
        ITitleBarContentProvider titleBarContentProvider)
    {
        _componentRenderer = componentRenderer ?? throw new ArgumentNullException(nameof(componentRenderer));
        _stateUpdater = stateUpdater ?? throw new ArgumentNullException(nameof(stateUpdater));
        _displayDimensions = displayDimensions ?? throw new ArgumentNullException(nameof(displayDimensions));
        _nowPlayingProvider = nowPlayingProvider ?? throw new ArgumentNullException(nameof(nowPlayingProvider));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _titleBarContentProvider = titleBarContentProvider ?? throw new ArgumentNullException(nameof(titleBarContentProvider));

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
        var titleViewport = new Viewport("", () => _titleBarContentProvider.GetTitleBarContent(), preformattedAnsi: true);
        _titleRow.SetRowData([titleViewport], [context.Width]);
        _row2.SetRowData(BuildRow2Viewports(), BuildRow2Widths(context.Width));
        _row3.SetRowData(BuildRow3Viewports(), BuildRow3Widths(context.Width));
        return [_titleRow, _row2, _row3];
    }

    private RenderContext BuildContext(string deviceName, bool invalidateCache)
    {
        int width = Math.Max(10, _displayDimensions.Width);
        var palette = _uiSettings.Palette ?? new UiPalette();
        return new RenderContext
        {
            Width = width,
            StartRow = 0,
            MaxLines = 3,
            Palette = palette,
            ScrollSpeed = _uiSettings.DefaultScrollingSpeed,
            DeviceName = deviceName,
            InvalidateWriteCache = invalidateCache
        };
    }

    private IReadOnlyList<Viewport> BuildRow2Viewports()
    {
        var palette = _uiSettings.Palette ?? new UiPalette();
        var nowTextColor = !string.IsNullOrEmpty(_nowPlayingText) ? palette.Highlighted : (PaletteColor?)null;
        return
        [
            new Viewport("Device", () => new PlainText(_deviceName)),
            new Viewport("Now", () => new PlainText(_nowPlayingText ?? ""), textColor: nowTextColor)
        ];
    }

    private static IReadOnlyList<int> BuildRow2Widths(int width)
    {
        int leftCellWidth = Math.Max(16, (width / 2 / 8) * 8);
        int rightCellWidth = width - leftCellWidth;
        return [leftCellWidth, rightCellWidth];
    }

    private IReadOnlyList<Viewport> BuildRow3Viewports()
    {
        string bpmLabel = _currentBpm >= 0 ? (_currentBpm > 0 ? "BPM" : "Beat") : "";
        return
        [
            _currentBpm >= 0
                ? new Viewport(bpmLabel, () => new PlainText(_bpmBeatValue))
                : new Viewport("", () => new PlainText("")),
            _volume >= 0
                ? new Viewport("Volume/dB", () => new PlainText(_volumeText))
                : new Viewport("", () => new PlainText(""))
        ];
    }

    private static IReadOnlyList<int> BuildRow3Widths(int width)
    {
        int bpmCellWidth = Math.Max(8, (width / 2 / 8) * 8);
        int volCellWidth = width - bpmCellWidth;
        return [bpmCellWidth, volCellWidth];
    }
}
