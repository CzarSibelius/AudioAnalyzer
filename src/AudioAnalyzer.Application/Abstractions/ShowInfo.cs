namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Metadata for one show in the repository.</summary>
/// <param name="Id">Stable id (e.g. filename without extension).</param>
/// <param name="Name">Display name from the show file, or id if name is missing.</param>
public sealed record ShowInfo(string Id, string Name);
