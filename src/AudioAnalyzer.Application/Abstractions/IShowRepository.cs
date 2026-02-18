using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Provides access to shows stored in the shows directory (e.g. JSON files).
/// </summary>
public interface IShowRepository
{
    /// <summary>Returns all available shows. Order is stable (e.g. alphabetical by id).</summary>
    IReadOnlyList<ShowInfo> GetAll();

    /// <summary>Loads a single show by id. Returns null if not found or invalid.</summary>
    Show? GetById(string id);

    /// <summary>Persists a show to shows/{id}.json.</summary>
    void Save(string id, Show show);

    /// <summary>Creates a new show file with generated id, saves it, and returns the id.</summary>
    string Create(Show show);

    /// <summary>Deletes the show file. No-op if file does not exist.</summary>
    void Delete(string id);
}
