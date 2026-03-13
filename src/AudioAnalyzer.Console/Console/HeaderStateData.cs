namespace AudioAnalyzer.Console;

/// <summary>State applied to <see cref="HeaderContainer"/> by <see cref="HeaderContainerStateUpdater"/>.</summary>
internal readonly struct HeaderStateData
{
    public string? DeviceName { get; init; }
    public string? NowPlayingText { get; init; }
    public double CurrentBpm { get; init; }
    public double BeatSensitivity { get; init; }
    public bool BeatFlashActive { get; init; }
    public float Volume { get; init; }
    public string? BpmBeatValue { get; init; }
    public string? VolumeText { get; init; }
}
