using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Updates <see cref="HeaderContainer"/> state from <see cref="RenderContext"/> and engine/now-playing data.
/// </summary>
internal sealed class HeaderContainerStateUpdater : IUiStateUpdater<HeaderContainer>
{
    private readonly INowPlayingProvider _nowPlayingProvider;
    private readonly AnalysisEngine _engine;
    private readonly UiSettings _uiSettings;
    private readonly AppSettings _appSettings;
    private readonly ILinkSession _linkSession;

    public HeaderContainerStateUpdater(
        INowPlayingProvider nowPlayingProvider,
        AnalysisEngine engine,
        UiSettings uiSettings,
        AppSettings appSettings,
        ILinkSession linkSession)
    {
        _nowPlayingProvider = nowPlayingProvider ?? throw new ArgumentNullException(nameof(nowPlayingProvider));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _linkSession = linkSession ?? throw new ArgumentNullException(nameof(linkSession));
    }

    /// <inheritdoc />
    public void Update(HeaderContainer component, RenderContext context)
    {
        string deviceName = context?.DeviceName ?? "";
        string? nowPlayingText = _nowPlayingProvider.GetNowPlaying()?.ToDisplayString();
        double currentBpm = _engine.CurrentBpm;
        double beatSensitivity = _engine.BeatSensitivity;
        bool beatFlashActive = _engine.BeatFlashActive;
        float volume = _engine.Volume;

        string suffix = beatFlashActive ? " *BEAT*" : "";
        string bpmBeatValue = _appSettings.BpmSource switch
        {
            BpmSource.AudioAnalysis => currentBpm > 0
                ? $"{currentBpm:F0}  Beat: {beatSensitivity:F1} (+/-){suffix}"
                : $"{beatSensitivity:F1} (+/-){suffix}",
            BpmSource.DemoDevice => currentBpm > 0
                ? $"{currentBpm:F0} (Demo){suffix}"
                : $"— (Demo){suffix}",
            BpmSource.AbletonLink => FormatLinkBpmLine(currentBpm, suffix),
            _ => $"{currentBpm:F0}{suffix}"
        };

        string volumeText;
        if (volume >= 0)
        {
            double db = 20 * Math.Log10(Math.Max(volume, 0.00001f));
            volumeText = $"{volume * 100:F1}% {db:F1}dB";
        }
        else
        {
            volumeText = "";
        }

        component.ApplyState(new HeaderStateData
        {
            DeviceName = deviceName,
            NowPlayingText = nowPlayingText,
            CurrentBpm = currentBpm,
            BeatSensitivity = beatSensitivity,
            BeatFlashActive = beatFlashActive,
            Volume = volume,
            BpmBeatValue = bpmBeatValue,
            VolumeText = volumeText
        });
    }

    private string FormatLinkBpmLine(double currentBpm, string suffix)
    {
        if (!_linkSession.IsAvailable)
        {
            return $"Link (no native DLL){suffix}";
        }

        int peers = 0;
        if (_linkSession.IsEnabled)
        {
            _linkSession.Capture(out _, out peers, out _, 4.0);
        }

        string peerPart = $" peers:{peers}";
        if (currentBpm >= 1.0)
        {
            return $"{currentBpm:F0} Link{peerPart}{suffix}";
        }

        return $"— Link{peerPart}{suffix}";
    }
}
