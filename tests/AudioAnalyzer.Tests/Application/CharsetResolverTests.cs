using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

/// <summary>Tests for <see cref="CharsetResolver"/> (ADR-0080).</summary>
public sealed class CharsetResolverTests
{
    private sealed class FakeCharsetRepo : ICharsetRepository
    {
        private readonly Dictionary<string, CharsetDefinition> _defs;

        public FakeCharsetRepo(IReadOnlyDictionary<string, CharsetDefinition> defs) =>
            _defs = new Dictionary<string, CharsetDefinition>(defs, StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<CharsetInfo> GetAll() =>
            _defs.Select(kv => new CharsetInfo(kv.Key, kv.Value.Name ?? kv.Key)).OrderBy(x => x.Id).ToList();

        public CharsetDefinition? GetById(string id) =>
            _defs.TryGetValue(id, out var d) ? d : null;

        public void Save(string id, CharsetDefinition definition) => _defs[id] = definition;

        public string Create(CharsetDefinition definition) => throw new NotSupportedException();
    }

    [Fact]
    public void ResolveByIdOrDefault_UsesDefaultId_WhenCharsetIdNull()
    {
        var repo = new FakeCharsetRepo(new Dictionary<string, CharsetDefinition>
        {
            [CharsetIds.AsciiRampClassic] = new CharsetDefinition { Name = "Classic", Characters = "AB" }
        });
        var r = new CharsetResolver(repo);
        Assert.Equal("AB", r.ResolveByIdOrDefault(null, CharsetIds.AsciiRampClassic, "ZZ"));
    }

    [Fact]
    public void ResolveByIdOrDefault_UsesExplicitId_WhenCharsetIdSet()
    {
        var repo = new FakeCharsetRepo(new Dictionary<string, CharsetDefinition>
        {
            ["digits"] = new CharsetDefinition { Characters = "012" },
            [CharsetIds.AsciiRampClassic] = new CharsetDefinition { Characters = "AB" }
        });
        var r = new CharsetResolver(repo);
        Assert.Equal("012", r.ResolveByIdOrDefault("digits", CharsetIds.AsciiRampClassic, "ZZ"));
    }
}
