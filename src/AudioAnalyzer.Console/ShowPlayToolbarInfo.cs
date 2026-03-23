using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Supplies Show play entry index for the compact Show mode toolbar.
/// </summary>
internal sealed class ShowPlayToolbarInfo : IShowPlayToolbarInfo
{
    private readonly VisualizerSettings _visualizerSettings;
    private readonly ShowPlaybackController _playback;
    private readonly IShowRepository _showRepository;

    public ShowPlayToolbarInfo(
        VisualizerSettings visualizerSettings,
        ShowPlaybackController playback,
        IShowRepository showRepository)
    {
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _playback = playback ?? throw new ArgumentNullException(nameof(playback));
        _showRepository = showRepository ?? throw new ArgumentNullException(nameof(showRepository));
    }

    /// <inheritdoc />
    public int CurrentEntryIndex => _playback.CurrentEntryIndex;

    /// <inheritdoc />
    public int GetActiveShowEntryCount()
    {
        string? id = _visualizerSettings.ActiveShowId;
        if (string.IsNullOrWhiteSpace(id))
        {
            return 0;
        }

        var show = _showRepository.GetById(id);
        return show?.Entries?.Count ?? 0;
    }
}
