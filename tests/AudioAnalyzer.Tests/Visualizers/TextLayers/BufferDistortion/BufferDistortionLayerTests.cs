using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.BufferDistortion;

/// <summary>Deterministic tests for buffer snapshot + plane-wave sampling (see BufferDistortionLayer).</summary>
public sealed class BufferDistortionLayerTests
{
    [Fact]
    public void PlaneWaveAlongX_at_pi_over_two_samples_vertical_offset_by_amplitude()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(5, 5);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));
        buffer.Set(0, 0, 'a', PaletteColor.FromRgb(1, 0, 0));
        buffer.Set(0, 2, 'b', PaletteColor.FromRgb(0, 1, 0));

        var store = new TextLayerStateStore();
        var layerRenderer = new BufferDistortionLayer(store);
        BufferDistortionState st = ((ITextLayerStateStore<BufferDistortionState>)store).GetState(0);
        st.PlanePhase = Math.PI / 2.0;

        var layer = new TextLayerSettings
        {
            LayerType = TextLayerType.BufferDistortion,
            SpeedMultiplier = 1.0
        };
        layer.SetCustom(new BufferDistortionSettings
        {
            Mode = BufferDistortionMode.PlaneWaves,
            PlaneOrientation = BufferDistortionPlaneOrientation.WaveAlongX,
            PlaneAmplitudeCells = 2,
            PlaneWavelengthCells = 24,
            PlanePhaseSpeed = 0.0
        });

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = new AudioAnalysisSnapshot() },
            Palette = [PaletteColor.FromRgb(0, 0, 0)],
            SpeedBurst = 1.0,
            ViewportWidth = 5,
            ViewportHeight = 5,
            Width = 5,
            Height = 5,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var tupleState = (0.0, 0);
        layerRenderer.Draw(layer, ref tupleState, ctx);

        var (c, _) = buffer.Get(0, 0);
        Assert.Equal('b', c);
    }

    [Fact]
    public void RenderBounds_restricts_warp_to_sub_rectangle()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(8, 4);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));
        buffer.Set(6, 1, 'x', PaletteColor.FromRgb(2, 0, 0));
        buffer.Set(6, 3, 'y', PaletteColor.FromRgb(0, 2, 0));

        var store = new TextLayerStateStore();
        var layerRenderer = new BufferDistortionLayer(store);
        BufferDistortionState st2 = ((ITextLayerStateStore<BufferDistortionState>)store).GetState(0);
        st2.PlanePhase = Math.PI / 2.0;

        var layer = new TextLayerSettings
        {
            LayerType = TextLayerType.BufferDistortion,
            SpeedMultiplier = 1.0,
            RenderBounds = new TextLayerRenderBounds { X = 0.5, Y = 0, Width = 0.5, Height = 1.0 }
        };
        layer.SetCustom(new BufferDistortionSettings
        {
            Mode = BufferDistortionMode.PlaneWaves,
            PlaneOrientation = BufferDistortionPlaneOrientation.WaveAlongX,
            PlaneAmplitudeCells = 2,
            PlaneWavelengthCells = 24,
            PlanePhaseSpeed = 0.0
        });

        var (rx, ry, rw, rh) = TextLayerRenderBounds.ToPixelRect(layer.RenderBounds, 8, 4);
        Assert.True(rw >= 2 && rh >= 2);

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = new AudioAnalysisSnapshot() },
            Palette = [PaletteColor.FromRgb(0, 0, 0)],
            SpeedBurst = 1.0,
            ViewportWidth = 8,
            ViewportHeight = 4,
            Width = rw,
            Height = rh,
            BufferOriginX = rx,
            BufferOriginY = ry,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var tupleState = (0.0, 0);
        layerRenderer.Draw(layer, ref tupleState, ctx);

        int lx = 6 - rx;
        int ly = 1 - ry;
        Assert.InRange(lx, 0, rw - 1);
        Assert.InRange(ly, 0, rh - 1);

        var (cWarped, _) = buffer.Get(6, 1);
        Assert.Equal('y', cWarped);

        var (cLeft, _) = buffer.Get(0, 0);
        Assert.Equal(' ', cLeft);
    }
}
