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

    public HeaderContainerStateUpdater(
        INowPlayingProvider nowPlayingProvider,
        AnalysisEngine engine,
        UiSettings uiSettings)
    {
        _nowPlayingProvider = nowPlayingProvider ?? throw new ArgumentNullException(nameof(nowPlayingProvider));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _uiSettings = uiSettings ?? throw new ArgumentNullException(nameof(uiSettings));
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
        string bpmBeatValue = currentBpm > 0
            ? $"{currentBpm:F0}  Beat: {beatSensitivity:F1} (+/-){suffix}"
            : $"{beatSensitivity:F1} (+/-){suffix}";

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
}
