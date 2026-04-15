using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application;

/// <summary>Resolves ordered character sequences from <see cref="ICharsetRepository"/> with safe fallbacks (ADR-0080).</summary>
public sealed class CharsetResolver
{
    private readonly ICharsetRepository _repository;

    /// <summary>Creates a resolver using the given repository.</summary>
    public CharsetResolver(ICharsetRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>When <paramref name="charsetId"/> is null/empty, uses <paramref name="defaultId"/>; then loads from the repository or returns <paramref name="literalFallback"/>.</summary>
    public string ResolveByIdOrDefault(string? charsetId, string defaultId, string literalFallback)
    {
        string id = string.IsNullOrWhiteSpace(charsetId) ? defaultId : charsetId.Trim();
        return ResolveById(id, literalFallback);
    }

    /// <summary>Loads characters for <paramref name="charsetId"/> or returns <paramref name="literalFallback"/> if missing/invalid.</summary>
    public string ResolveById(string charsetId, string literalFallback)
    {
        if (string.IsNullOrWhiteSpace(charsetId))
        {
            return literalFallback;
        }

        var def = _repository.GetById(charsetId.Trim());
        return def?.Characters is { Length: > 0 } s ? s : literalFallback;
    }

}
