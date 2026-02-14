namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Metadata for one palette in the repository.</summary>
/// <param name="Id">Stable id (e.g. filename without extension).</param>
/// <param name="Name">Display name from the palette file, or id if name is missing.</param>
public sealed record PaletteInfo(string Id, string Name);
