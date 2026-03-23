using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests;

/// <summary>Unit tests for <see cref="PaletteSwatchFormatter"/>.</summary>
public sealed class PaletteSwatchFormatterTests
{
    private static readonly PaletteColor C1 = PaletteColor.FromRgb(10, 20, 30);
    private static readonly PaletteColor C2 = PaletteColor.FromRgb(200, 100, 50);

    [Fact]
    public void FormatPaletteColoredName_empty_colors_returns_plain()
    {
        Assert.Equal("ab", PaletteSwatchFormatter.FormatPaletteColoredName("ab", null, 0));
        Assert.Equal("ab", PaletteSwatchFormatter.FormatPaletteColoredName("ab", Array.Empty<PaletteColor>(), 0));
    }

    [Fact]
    public void FormatPaletteColoredName_two_colors_phase0_alternates()
    {
        var colors = new[] { C1, C2 };
        var s = PaletteSwatchFormatter.FormatPaletteColoredName("ab", colors, 0);
        Assert.StartsWith("\x1b[38;2;10;20;30m", s);
        Assert.Contains("a", s);
        Assert.Contains("\x1b[38;2;200;100;50m", s);
        Assert.EndsWith("b\x1b[0m", s);
    }

    [Fact]
    public void FormatPaletteColoredName_phase1_swaps_which_letter_gets_first_color()
    {
        var colors = new[] { C1, C2 };
        var s0 = PaletteSwatchFormatter.FormatPaletteColoredName("ab", colors, 0);
        var s1 = PaletteSwatchFormatter.FormatPaletteColoredName("ab", colors, 1);
        Assert.NotEqual(s0, s1);
        Assert.StartsWith("\x1b[38;2;200;100;50m", s1);
    }

    [Fact]
    public void ComputeToolbarPhaseOffset_low_bpm_uses_tick_bucket()
    {
        var snap = new AnalysisSnapshot { CurrentBpm = 0, BeatCount = 99 };
        int p = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(snap, 5);
        Assert.InRange(p, 0, 4);
    }

    [Fact]
    public void ComputeToolbarPhaseOffset_with_bpm_uses_beat_count_mod()
    {
        var snap = new AnalysisSnapshot { CurrentBpm = 120, BeatCount = 7 };
        int p = PaletteSwatchFormatter.ComputeToolbarPhaseOffset(snap, 5);
        Assert.Equal(7 % 5, p);
    }
}
