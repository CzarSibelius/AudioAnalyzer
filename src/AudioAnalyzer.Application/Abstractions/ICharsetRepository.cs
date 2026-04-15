using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>
/// Provides access to charset JSON files in the <c>charsets</c> directory (ADR-0080).
/// </summary>
public interface ICharsetRepository
{
    /// <summary>Returns all charsets (stable order, e.g. by id).</summary>
    IReadOnlyList<CharsetInfo> GetAll();

    /// <summary>Loads a charset by id. Returns null if missing or invalid.</summary>
    CharsetDefinition? GetById(string id);

    /// <summary>Writes <paramref name="definition"/> to <c>charsets/{id}.json</c>.</summary>
    void Save(string id, CharsetDefinition definition);

    /// <summary>Creates a new charset file with a generated unique id and returns that id.</summary>
    string Create(CharsetDefinition definition);
}
