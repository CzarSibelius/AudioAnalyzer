using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Console;

/// <summary>
/// Tracks Show playback state and advances to the next preset when duration is exceeded.
/// Call Tick() each frame when in Show play mode.
/// </summary>
internal sealed class ShowPlaybackController
{
    private readonly VisualizerSettings _visualizerSettings;
    private readonly IShowRepository _showRepo;
    private readonly IPresetRepository _presetRepo;
    private readonly AnalysisEngine _engine;

    private DateTime _entryStartTime = DateTime.UtcNow;
    private int _beatCountAtEntry;
    private int _currentEntryIndex;

    public ShowPlaybackController(
        VisualizerSettings visualizerSettings,
        IShowRepository showRepo,
        IPresetRepository presetRepo,
        AnalysisEngine engine)
    {
        _visualizerSettings = visualizerSettings ?? throw new ArgumentNullException(nameof(visualizerSettings));
        _showRepo = showRepo ?? throw new ArgumentNullException(nameof(showRepo));
        _presetRepo = presetRepo ?? throw new ArgumentNullException(nameof(presetRepo));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <summary>Advances to the next preset if current entry duration has elapsed. Call each frame during Show play.</summary>
    public void Tick()
    {
        var showId = _visualizerSettings.ActiveShowId;
        if (string.IsNullOrWhiteSpace(showId))
        {
            return;
        }

        var show = _showRepo.GetById(showId);
        if (show?.Entries is not { Count: > 0 })
        {
            return;
        }

        if (_currentEntryIndex >= show.Entries.Count)
        {
            _currentEntryIndex = 0;
        }

        var entry = show.Entries[_currentEntryIndex];
        var preset = _presetRepo.GetById(entry.PresetId);
        if (preset == null)
        {
            return;
        }

        var (duration, elapsed) = GetDurationAndElapsed(entry);
        if (elapsed >= duration)
        {
            AdvanceToNext(show);
        }
    }

    /// <summary>Resets playback to the first entry. Call when entering Show play mode.</summary>
    public void Reset()
    {
        _currentEntryIndex = 0;
        _entryStartTime = DateTime.UtcNow;
        _beatCountAtEntry = _engine.BeatCount;
    }

    /// <summary>Loads the current Show entry's preset into TextLayers. Call when entering Show play or after Reset.</summary>
    public void LoadCurrentEntry()
    {
        var showId = _visualizerSettings.ActiveShowId;
        if (string.IsNullOrWhiteSpace(showId))
        {
            return;
        }

        var show = _showRepo.GetById(showId);
        if (show?.Entries is not { Count: > 0 })
        {
            return;
        }

        _currentEntryIndex = Math.Clamp(_currentEntryIndex, 0, show.Entries.Count - 1);
        var entry = show.Entries[_currentEntryIndex];
        var preset = _presetRepo.GetById(entry.PresetId);
        if (preset != null)
        {
            _visualizerSettings.ActivePresetId = preset.Id;
            _visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
            _visualizerSettings.TextLayers.CopyFrom(preset.Config);
            _entryStartTime = DateTime.UtcNow;
            _beatCountAtEntry = _engine.BeatCount;
        }
    }

    /// <summary>Current entry index within the Show (0-based).</summary>
    public int CurrentEntryIndex => _currentEntryIndex;

    private (double Duration, double Elapsed) GetDurationAndElapsed(ShowEntry entry)
    {
        var config = entry.Duration ?? new DurationConfig { Unit = DurationUnit.Seconds, Value = 30 };
        if (config.Unit == DurationUnit.Beats)
        {
            var durationBeats = config.Value;
            var elapsedBeats = _engine.BeatCount - _beatCountAtEntry;
            return (durationBeats, elapsedBeats);
        }
        var durationSeconds = config.Value;
        var elapsedSeconds = (DateTime.UtcNow - _entryStartTime).TotalSeconds;
        return (durationSeconds, elapsedSeconds);
    }

    private void AdvanceToNext(Show show)
    {
        _currentEntryIndex = (_currentEntryIndex + 1) % show.Entries.Count;
        var entry = show.Entries[_currentEntryIndex];
        var preset = _presetRepo.GetById(entry.PresetId);
        if (preset != null)
        {
            _visualizerSettings.ActivePresetId = preset.Id;
            _visualizerSettings.TextLayers ??= new TextLayersVisualizerSettings();
            _visualizerSettings.TextLayers.CopyFrom(preset.Config);
            _entryStartTime = DateTime.UtcNow;
            _beatCountAtEntry = _engine.BeatCount;
        }
    }
}
