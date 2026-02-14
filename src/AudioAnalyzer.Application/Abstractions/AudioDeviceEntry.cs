namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Display name and an opaque id for creating capture.</summary>
public sealed class AudioDeviceEntry
{
    public string Name { get; init; } = "";
    public string? Id { get; init; }
}
