using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Provides access to palettes stored in the palettes directory (e.g. JSON files).
/// </summary>
public interface IPaletteRepository
{
    /// <summary>Returns all available palettes (id = filename without extension, display name). Order is stable (e.g. alphabetical by id).</summary>
    IReadOnlyList<PaletteInfo> GetAll();

    /// <summary>Loads a single palette by id (e.g. filename without extension). Returns null if not found or invalid.</summary>
    PaletteDefinition? GetById(string id);
}

/// <summary>Metadata for one palette in the repository.</summary>
/// <param name="Id">Stable id (e.g. filename without extension).</param>
/// <param name="Name">Display name from the palette file, or id if name is missing.</param>
public sealed record PaletteInfo(string Id, string Name);
