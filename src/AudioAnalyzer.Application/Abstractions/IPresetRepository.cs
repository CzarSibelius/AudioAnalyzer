using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Provides access to presets stored in the presets directory (e.g. JSON files).
/// </summary>
public interface IPresetRepository
{
    /// <summary>Returns all available presets (id = filename without extension, display name). Order is stable (e.g. alphabetical by id).</summary>
    IReadOnlyList<PresetInfo> GetAll();

    /// <summary>Loads a single preset by id (e.g. filename without extension). Returns null if not found or invalid.</summary>
    Preset? GetById(string id);

    /// <summary>Persists a preset to presets/{id}.json.</summary>
    void Save(string id, Preset preset);

    /// <summary>Creates a new preset file with generated id, saves it, and returns the id.</summary>
    string Create(Preset preset);

    /// <summary>Deletes the preset file. No-op if file does not exist.</summary>
    void Delete(string id);
}
