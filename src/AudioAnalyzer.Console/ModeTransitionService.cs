using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Implements Tab cycling: Preset editor → Show play (if eligible) → General settings → Preset editor.
/// </summary>
internal sealed class ModeTransitionService : IModeTransitionService
{
    private readonly IVisualizerSettingsRepository _visualizerSettingsRepo;
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IShowRepository _showRepository;
    private readonly IDisplayState _displayState;
    private readonly GeneralSettingsHubState _generalSettingsHubState;
    private readonly ShowPlaybackController _showPlaybackController;

    public ModeTransitionService(
        IVisualizerSettingsRepository visualizerSettingsRepo,
        VisualizerSettings visualizerSettings,
        IShowRepository showRepository,
        IDisplayState displayState,
        GeneralSettingsHubState generalSettingsHubState,
        ShowPlaybackController showPlaybackController)
    {
        _visualizerSettingsRepo = visualizerSettingsRepo ?? throw new ArgumentNullException(nameof(visualizerSettingsRepo));
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _showRepository = showRepository ?? throw new ArgumentNullException(nameof(showRepository));
        _displayState = displayState ?? throw new ArgumentNullException(nameof(displayState));
        _generalSettingsHubState = generalSettingsHubState ?? throw new ArgumentNullException(nameof(generalSettingsHubState));
        _showPlaybackController = showPlaybackController ?? throw new ArgumentNullException(nameof(showPlaybackController));
    }

    /// <inheritdoc />
    public void CycleToNextMode()
    {
        var mode = _visualizerSettings.ApplicationMode;

        if (mode == ApplicationMode.Settings)
        {
            _displayState.FullScreen = false;
            _visualizerSettings.ApplicationMode = ApplicationMode.PresetEditor;
            _visualizerSettings.ActiveShowId = null;
            _visualizerSettings.ActiveShowName = null;
            _generalSettingsHubState.ResetInteraction();
            _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings);
            return;
        }

        if (mode == ApplicationMode.ShowPlay)
        {
            _displayState.FullScreen = false;
            _visualizerSettings.ApplicationMode = ApplicationMode.Settings;
            _generalSettingsHubState.ResetInteraction();
            _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings);
            return;
        }

        if (TryResolveShowForPlay(out string showId, out var show))
        {
            _displayState.FullScreen = false;
            _visualizerSettings.ApplicationMode = ApplicationMode.ShowPlay;
            _visualizerSettings.ActiveShowId = showId;
            _visualizerSettings.ActiveShowName = show.Name?.Trim();
            _showPlaybackController.Reset();
            _showPlaybackController.LoadCurrentEntry();
            _generalSettingsHubState.ResetInteraction();
            _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings);
            return;
        }

        _displayState.FullScreen = false;
        _visualizerSettings.ApplicationMode = ApplicationMode.Settings;
        _generalSettingsHubState.ResetInteraction();
        _visualizerSettingsRepo.SaveVisualizerSettings(_visualizerSettings);
    }

    private bool TryResolveShowForPlay(out string showId, out Show show)
    {
        showId = "";
        show = null!;
        var allShows = _showRepository.GetAll();
        if (allShows.Count == 0)
        {
            return false;
        }

        string candidateId = _visualizerSettings.ActiveShowId ?? allShows[0].Id;
        var resolved = _showRepository.GetById(candidateId);
        if (resolved == null || resolved.Entries is not { Count: > 0 })
        {
            resolved = null;
            foreach (var info in allShows)
            {
                var s = _showRepository.GetById(info.Id);
                if (s?.Entries is { Count: > 0 })
                {
                    resolved = s;
                    break;
                }
            }
        }

        if (resolved == null || resolved.Entries is not { Count: > 0 })
        {
            return false;
        }

        showId = resolved.Id;
        show = resolved;
        return true;
    }
}
