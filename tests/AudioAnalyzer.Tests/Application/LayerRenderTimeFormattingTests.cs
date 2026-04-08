using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application;

/// <summary>ADR-0073: compact strings for S modal layer timing suffix.</summary>
public sealed class LayerRenderTimeFormattingTests
{
    [Fact]
    public void FormatEntrySuffix_Null_ReturnsEmDash()
    {
        Assert.Equal(" \u2014", LayerRenderTimeFormatting.FormatEntrySuffix(null));
    }

    [Fact]
    public void FormatEntrySuffix_TinyValue_UsesFloorLabel()
    {
        Assert.Equal(" <0.01ms", LayerRenderTimeFormatting.FormatEntrySuffix(0.001));
    }

    [Fact]
    public void FormatEntrySuffix_SubTenMs_UsesTwoSignificantStyle()
    {
        string s = LayerRenderTimeFormatting.FormatEntrySuffix(1.234);
        Assert.Equal(" 1.23ms", s);
    }

    [Fact]
    public void FormatEntrySuffix_Large_RoundsToIntegerMs()
    {
        Assert.Equal(" 15ms", LayerRenderTimeFormatting.FormatEntrySuffix(14.6));
    }

    [Fact]
    public void PerLayerBudgetMsFor60Fps_DividesFrameByEnabledCount()
    {
        double frame = LayerRenderTimeFormatting.FrameBudgetMsAt60Fps;
        Assert.Equal(frame, LayerRenderTimeFormatting.PerLayerBudgetMsFor60Fps(1), 9);
        Assert.Equal(frame / 3, LayerRenderTimeFormatting.PerLayerBudgetMsFor60Fps(3), 9);
        Assert.Equal(frame, LayerRenderTimeFormatting.PerLayerBudgetMsFor60Fps(0), 9);
    }

    [Fact]
    public void GetTierForeground_UsesPaletteSlotsByRatioToBudget()
    {
        var p = new UiPalette
        {
            Highlighted = PaletteColor.FromConsoleColor(ConsoleColor.Yellow),
            Normal = PaletteColor.FromConsoleColor(ConsoleColor.White),
            Dimmed = PaletteColor.FromConsoleColor(ConsoleColor.DarkGray)
        };

        double budget = 5.0;
        Assert.Equal(p.Dimmed, LayerRenderTimeFormatting.GetTierForeground(p, null, budget));
        Assert.Equal(p.Dimmed, LayerRenderTimeFormatting.GetTierForeground(p, 1.0, budget));
        Assert.Equal(p.Normal, LayerRenderTimeFormatting.GetTierForeground(p, 2.0, budget));
        Assert.Equal(p.Highlighted, LayerRenderTimeFormatting.GetTierForeground(p, 6.0, budget));
    }
}
