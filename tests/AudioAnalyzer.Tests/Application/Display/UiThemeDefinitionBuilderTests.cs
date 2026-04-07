using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Palette;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Display;

/// <summary>Tests for <see cref="UiThemeDefinitionBuilder"/>.</summary>
public sealed class UiThemeDefinitionBuilderTests
{
    [Fact]
    public void FromPaletteSlotIndices_WritesAllSlots()
    {
        var colors = new[]
        {
            PaletteColor.FromRgb(1, 0, 0),
            PaletteColor.FromRgb(0, 2, 0),
            PaletteColor.FromRgb(0, 0, 3)
        };
        ReadOnlySpan<int> idx = [0, 1, 2, 0, 1, 2, 0, 1, 2, 0, 1];

        UiThemeDefinition def = UiThemeDefinitionBuilder.FromPaletteSlotIndices("My", "srcPal", colors, idx);

        Assert.Equal("My", def.Name);
        Assert.Equal("srcPal", def.FallbackPaletteId);
        Assert.NotNull(def.Ui);
        Assert.NotNull(def.TitleBar);
        PaletteColor uiNormal = ColorPaletteParser.ParseEntry(def.Ui!.Normal);
        Assert.True(uiNormal.IsRgb && uiNormal.R == 1 && uiNormal.G == 0 && uiNormal.B == 0);
        PaletteColor mode = ColorPaletteParser.ParseEntry(def.TitleBar!.Mode);
        Assert.True(mode.IsRgb && mode.R == 1 && mode.G == 0 && mode.B == 0);
    }
}
