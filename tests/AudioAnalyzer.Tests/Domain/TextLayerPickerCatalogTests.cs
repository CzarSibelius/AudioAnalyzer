using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Domain;

public sealed class TextLayerPickerCatalogTests
{
    [Fact]
    public void OrderedTypes_contains_every_TextLayerType_once()
    {
        var all = Enum.GetValues<TextLayerType>().ToHashSet();
        var catalog = TextLayerPickerCatalog.OrderedTypes.ToHashSet();
        Assert.Equal(all.Count, TextLayerPickerCatalog.OrderedTypes.Count);
        Assert.Equal(all, catalog);
    }

    [Fact]
    public void OrderedTypes_is_sorted_case_insensitively_by_name()
    {
        var names = TextLayerPickerCatalog.OrderedTypes.Select(t => t.ToString()).ToArray();
        var sorted = names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
        Assert.Equal(sorted, names);
    }
}
