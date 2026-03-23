using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Display;

/// <summary>Tests for <see cref="UiThemePaletteMapper"/>.</summary>
public sealed class UiThemePaletteMapperTests
{
    [Fact]
    public void Map_UsesModulo_WhenFewerThanElevenColors()
    {
        var colors = new[]
        {
            PaletteColor.FromRgb(1, 0, 0),
            PaletteColor.FromRgb(0, 2, 0)
        };

        (UiPalette ui, TitleBarPalette tb) = UiThemePaletteMapper.Map(colors);

        Assert.Equal(colors[0], ui.Normal);
        Assert.Equal(colors[1], ui.Highlighted);
        Assert.Equal(colors[0], ui.Dimmed);
        Assert.Equal(colors[1], ui.Label);
        Assert.Equal(colors[0], ui.Background);
        Assert.Equal(colors[1], tb.AppName);
        Assert.Equal(colors[0], tb.Mode);
        Assert.Equal(colors[1], tb.Preset);
        Assert.Equal(colors[0], tb.Layer);
        Assert.Equal(colors[1], tb.Separator);
        Assert.Equal(colors[0], tb.Frame);
    }

    [Fact]
    public void Map_MapsElevenDistinctIndices_WhenElevenColors()
    {
        var colors = Enumerable.Range(0, 11)
            .Select(i => PaletteColor.FromRgb((byte)i, 0, 0))
            .ToArray();

        (UiPalette ui, TitleBarPalette tb) = UiThemePaletteMapper.Map(colors);

        for (int i = 0; i < 5; i++)
        {
            PaletteColor expected = colors[i];
            PaletteColor actual = i switch
            {
                0 => ui.Normal,
                1 => ui.Highlighted,
                2 => ui.Dimmed,
                3 => ui.Label,
                4 => ui.Background!.Value,
                _ => throw new InvalidOperationException()
            };
            Assert.Equal(expected, actual);
        }

        Assert.Equal(colors[5], tb.AppName);
        Assert.Equal(colors[6], tb.Mode);
        Assert.Equal(colors[7], tb.Preset);
        Assert.Equal(colors[8], tb.Layer);
        Assert.Equal(colors[9], tb.Separator);
        Assert.Equal(colors[10], tb.Frame);
    }

    [Fact]
    public void Map_Throws_WhenEmpty()
    {
        Assert.Throws<ArgumentException>(() => UiThemePaletteMapper.Map(Array.Empty<PaletteColor>()));
    }
}
