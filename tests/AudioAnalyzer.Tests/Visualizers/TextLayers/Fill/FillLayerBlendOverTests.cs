using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Display;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.Fill;

/// <summary>Verifies Fill BlendOver reads per-cell under-color (not a single averaged slab).</summary>
public sealed class FillLayerBlendOverTests
{
    [Fact]
    public void BlendOver_output_differs_when_under_color_differs_in_region()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(10, 10);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));
        for (int y = 5; y < 10; y++)
        {
            buffer.Set(3, y, '█', PaletteColor.FromConsoleColor(ConsoleColor.Magenta));
        }

        var layer = new TextLayerSettings
        {
            LayerType = TextLayerType.Fill,
            ColorIndex = 0,
            RenderBounds = new TextLayerRenderBounds { X = 0, Y = 0.5, Width = 1, Height = 0.5 }
        };
        layer.SetCustom(new FillSettings
        {
            FillCompositeMode = FillCompositeMode.BlendOver,
            BlendStrength = 0.1,
            FillColorStyle = FillColorStyle.Solid
        });

        var palette = new List<PaletteColor> { PaletteColor.FromRgb(0, 0, 0) };
        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Snapshot = new AnalysisSnapshot(),
            Palette = palette,
            SpeedBurst = 1.0,
            Width = 10,
            Height = 10,
            LayerIndex = 2
        };

        var fill = new FillLayer();
        var state = (0.0, 0);
        fill.Draw(layer, ref state, ctx);

        var (_, gap) = buffer.Get(5, 5);
        var (_, bar) = buffer.Get(3, 5);
        Assert.NotEqual(gap, bar);
    }

    [Fact]
    public void BlendOver_space_cell_with_BlendSpaceAsBlack_uses_black_under_for_blend()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(10, 10);
        buffer.Clear(PaletteColor.FromConsoleColor(ConsoleColor.DarkMagenta));
        var layer = new TextLayerSettings
        {
            LayerType = TextLayerType.Fill,
            ColorIndex = 0,
            RenderBounds = null
        };
        layer.SetCustom(new FillSettings
        {
            FillCompositeMode = FillCompositeMode.BlendOver,
            BlendStrength = 0.1,
            FillColorStyle = FillColorStyle.Solid,
            BlendSpaceAsBlack = true
        });

        var palette = new List<PaletteColor> { PaletteColor.FromRgb(0, 0, 0) };
        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Snapshot = new AnalysisSnapshot(),
            Palette = palette,
            SpeedBurst = 1.0,
            Width = 10,
            Height = 10,
            LayerIndex = 0
        };

        var fill = new FillLayer();
        var state = (0.0, 0);
        fill.Draw(layer, ref state, ctx);

        var (_, outColor) = buffer.Get(5, 5);
        var rgb = PaletteColorBlending.ToRgb(outColor);
        Assert.Equal(0, rgb.R);
        Assert.Equal(0, rgb.G);
        Assert.Equal(0, rgb.B);
    }

    [Fact]
    public void BlendOver_space_cell_without_BlendSpaceAsBlack_keeps_clear_color_in_blend()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(10, 10);
        buffer.Clear(PaletteColor.FromConsoleColor(ConsoleColor.DarkMagenta));
        var layer = new TextLayerSettings
        {
            LayerType = TextLayerType.Fill,
            ColorIndex = 0,
            RenderBounds = null
        };
        layer.SetCustom(new FillSettings
        {
            FillCompositeMode = FillCompositeMode.BlendOver,
            BlendStrength = 0.1,
            FillColorStyle = FillColorStyle.Solid,
            BlendSpaceAsBlack = false
        });

        var palette = new List<PaletteColor> { PaletteColor.FromRgb(0, 0, 0) };
        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Snapshot = new AnalysisSnapshot(),
            Palette = palette,
            SpeedBurst = 1.0,
            Width = 10,
            Height = 10,
            LayerIndex = 0
        };

        var fill = new FillLayer();
        var state = (0.0, 0);
        fill.Draw(layer, ref state, ctx);

        var (_, outColor) = buffer.Get(5, 5);
        var rgb = PaletteColorBlending.ToRgb(outColor);
        Assert.True(rgb.R > 0 || rgb.B > 0);
    }
}
