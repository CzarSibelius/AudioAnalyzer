using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.WaveformStrip;

/// <summary>Verifies waveform strip uses layer-local dimensions when <see cref="TextLayerSettings.RenderBounds"/> is set.</summary>
public sealed class WaveformStripLayerRenderBoundsTests
{
    [Fact]
    public void Zero_waveform_centers_trace_vertically_in_render_bounds_not_full_viewport()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(10, 10);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var waveform = new float[32];
        var analysis = new AudioAnalysisSnapshot
        {
            Waveform = waveform,
            WaveformSize = 32,
            WaveformPosition = 0
        };

        var palette = new List<PaletteColor>
        {
            PaletteColor.FromConsoleColor(ConsoleColor.Cyan),
            PaletteColor.FromConsoleColor(ConsoleColor.Green)
        };

        var layer = new TextLayerSettings
        {
            LayerType = TextLayerType.WaveformStrip,
            RenderBounds = new TextLayerRenderBounds { X = 0, Y = 0.5, Width = 1, Height = 0.5 }
        };

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = 10,
            ViewportHeight = 10,
            Width = 10,
            Height = 5,
            BufferOriginX = 0,
            BufferOriginY = 5,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var strip = new WaveformStripLayer();
        var state = (0.0, 0);
        strip.Draw(layer, ref state, ctx);

        int localCenterY = 5 / 2;
        int bufferRowForCenter = 5 + localCenterY;
        var (ch, _) = buffer.Get(0, bufferRowForCenter);
        Assert.Equal('█', ch);

        var (topRowCh, _) = buffer.Get(0, 5);
        Assert.NotEqual('█', topRowCh);
    }
}
