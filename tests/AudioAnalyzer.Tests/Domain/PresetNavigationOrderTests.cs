using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Domain;

public sealed class PresetNavigationOrderTests
{
    [Fact]
    public void GetNextPresetIdByDisplayName_follows_trimmed_case_insensitive_order_with_id_tie_break()
    {
        var presets = new List<Preset>
        {
            new() { Id = "z", Name = "bravo" },
            new() { Id = "a", Name = "Alpha" },
            new() { Id = "m", Name = "alpha" }
        };

        // Sorted: Alpha (a), alpha (m) — id tie-break — then bravo (z).
        string? n1 = PresetNavigationOrder.GetNextPresetIdByDisplayName(presets, "z");
        Assert.Equal("a", n1);

        string? n2 = PresetNavigationOrder.GetNextPresetIdByDisplayName(presets, "a");
        Assert.Equal("m", n2);

        string? n3 = PresetNavigationOrder.GetNextPresetIdByDisplayName(presets, "m");
        Assert.Equal("z", n3);
    }

    [Fact]
    public void GetNextPresetIdByDisplayName_empty_name_uses_id_as_sort_key()
    {
        var presets = new List<Preset>
        {
            new() { Id = "bbb", Name = "   " },
            new() { Id = "aaa", Name = "   " }
        };

        var sorted = PresetNavigationOrder.SortForNavigation(presets);
        Assert.Equal("aaa", sorted[0].Id);
        Assert.Equal("bbb", sorted[1].Id);

        string? next = PresetNavigationOrder.GetNextPresetIdByDisplayName(presets, "aaa");
        Assert.Equal("bbb", next);
    }

    [Fact]
    public void GetNextPresetIdByDisplayName_unknown_active_starts_at_first_sorted()
    {
        var presets = new List<Preset>
        {
            new() { Id = "1", Name = "Zebra" },
            new() { Id = "2", Name = "Apple" }
        };

        string? next = PresetNavigationOrder.GetNextPresetIdByDisplayName(presets, "missing");
        Assert.Equal("2", next);
    }

    [Fact]
    public void GetPreviousPresetIdByDisplayName_is_inverse_of_next()
    {
        var presets = new List<Preset>
        {
            new() { Id = "z", Name = "bravo" },
            new() { Id = "a", Name = "Alpha" },
            new() { Id = "m", Name = "alpha" }
        };

        Assert.Equal("z", PresetNavigationOrder.GetPreviousPresetIdByDisplayName(presets, "a"));
        Assert.Equal("a", PresetNavigationOrder.GetPreviousPresetIdByDisplayName(presets, "m"));
        Assert.Equal("m", PresetNavigationOrder.GetPreviousPresetIdByDisplayName(presets, "z"));
    }

    [Fact]
    public void GetPreviousPresetIdByDisplayName_unknown_active_selects_last_sorted()
    {
        var presets = new List<Preset>
        {
            new() { Id = "1", Name = "Zebra" },
            new() { Id = "2", Name = "Apple" }
        };

        string? prev = PresetNavigationOrder.GetPreviousPresetIdByDisplayName(presets, "missing");
        Assert.Equal("1", prev);
    }
}
