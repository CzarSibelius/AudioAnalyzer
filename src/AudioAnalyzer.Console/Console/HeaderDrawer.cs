using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>Draws the application header using encapsulated viewports, title bar, engine state, and UI settings.</summary>
internal sealed class HeaderDrawer : IHeaderDrawer
{
    private readonly ITitleBarRenderer _titleBar;
    private readonly IScrollingTextViewport _deviceViewport;
    private readonly IScrollingTextViewport _nowPlayingViewport;
    private readonly INowPlayingProvider _nowPlayingProvider;
    private readonly AnalysisEngine _engine;
    private readonly UiSettings _uiSettings;

    public HeaderDrawer(
        ITitleBarRenderer titleBar,
        IScrollingTextViewport deviceViewport,
        IScrollingTextViewport nowPlayingViewport,
        INowPlayingProvider nowPlayingProvider,
        AnalysisEngine engine,
        UiSettings uiSettings)
    {
        _titleBar = titleBar ?? throw new ArgumentNullException(nameof(titleBar));
        _deviceViewport = deviceViewport ?? throw new ArgumentNullException(nameof(deviceViewport));
        _nowPlayingViewport = nowPlayingViewport ?? throw new ArgumentNullException(nameof(nowPlayingViewport));
        _nowPlayingProvider = nowPlayingProvider ?? throw new ArgumentNullException(nameof(nowPlayingProvider));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
    }

    /// <inheritdoc />
    public void DrawMain(string deviceName)
    {
        ConsoleHeader.DrawMain(
            deviceName,
            _titleBar,
            _deviceViewport,
            _nowPlayingViewport,
            _nowPlayingProvider.GetNowPlaying()?.ToDisplayString(),
            _uiSettings,
            _engine.CurrentBpm,
            _engine.BeatSensitivity,
            _engine.BeatFlashActive,
            _engine.Volume);
    }

    /// <inheritdoc />
    public void DrawHeaderOnly(string deviceName)
    {
        ConsoleHeader.DrawHeaderOnly(
            deviceName,
            _titleBar,
            _deviceViewport,
            _nowPlayingViewport,
            _nowPlayingProvider.GetNowPlaying()?.ToDisplayString(),
            _uiSettings,
            _engine.CurrentBpm,
            _engine.BeatSensitivity,
            _engine.BeatFlashActive,
            _engine.Volume);
    }
}
