namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Metadata for one preset in the repository.</summary>
/// <param name="Id">Stable id (e.g. filename without extension).</param>
/// <param name="Name">Display name from the preset file, or id if name is missing.</param>
public sealed record PresetInfo(string Id, string Name);
