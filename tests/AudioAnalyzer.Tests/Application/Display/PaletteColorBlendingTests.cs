using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Domain;
using Xunit;

namespace AudioAnalyzer.Tests.Application.Display;

/// <summary>Unit tests for <see cref="PaletteColorBlending"/>.</summary>
public sealed class PaletteColorBlendingTests
{
    [Fact]
    public void ToRgb_FromRgb_round_trips()
    {
        var c = PaletteColor.FromRgb(10, 20, 30);
        var rgb = PaletteColorBlending.ToRgb(c);
        Assert.Equal((10, 20, 30), rgb);
    }

    [Fact]
    public void ToRgb_Black_console_color_maps_to_zero()
    {
        var rgb = PaletteColorBlending.ToRgb(PaletteColor.FromConsoleColor(ConsoleColor.Black));
        Assert.Equal((0, 0, 0), rgb);
    }

    [Fact]
    public void BlendOver_halfway_between_white_and_black_is_mid_gray()
    {
        var white = PaletteColor.FromRgb(255, 255, 255);
        var black = PaletteColor.FromRgb(0, 0, 0);
        var blended = PaletteColorBlending.BlendOver(white, black, 0.5);
        Assert.True(blended.IsRgb);
        Assert.Equal(128, blended.R);
        Assert.Equal(128, blended.G);
        Assert.Equal(128, blended.B);
    }

    [Fact]
    public void LerpRgb_endpoints_match()
    {
        var a = ((byte)0, (byte)0, (byte)0);
        var b = ((byte)100, (byte)200, (byte)50);
        Assert.Equal(a, PaletteColorBlending.LerpRgb(a, b, 0.0));
        Assert.Equal(b, PaletteColorBlending.LerpRgb(a, b, 1.0));
    }
}
