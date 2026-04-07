using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Loads and saves UI theme JSON files in the themes directory (same pattern as presets).
/// </summary>
public interface IUiThemeRepository
{
    /// <summary>Returns all themes (id, display name, optional fallback palette id for swatches).</summary>
    IReadOnlyList<ThemeInfo> GetAll();

    /// <summary>Loads a theme by id. Returns null if missing or invalid.</summary>
    UiThemeDefinition? GetById(string id);

    /// <summary>Writes <paramref name="definition"/> to themes/{id}.json.</summary>
    void Save(string id, UiThemeDefinition definition);

    /// <summary>Creates a new theme file with a generated unique id and returns that id.</summary>
    string Create(UiThemeDefinition definition);
}
