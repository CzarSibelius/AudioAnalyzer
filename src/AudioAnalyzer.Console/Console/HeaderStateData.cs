namespace AudioAnalyzer.Console;

/// <summary>State applied to <see cref="HeaderContainer"/> by <see cref="HeaderContainerStateUpdater"/>.</summary>
internal readonly struct HeaderStateData
{
    public string? DeviceName { get; init; }
    public string? NowPlayingText { get; init; }
    public double CurrentBpm { get; init; }
    public float Volume { get; init; }
    public string? BpmCellValue { get; init; }
    public string? BeatCellValue { get; init; }
    public string? VolumeText { get; init; }
}
