using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.FractalZoom;

public sealed class FractalZoomLayerTests
{
    private sealed class EmptyCharsetRepo : ICharsetRepository
    {
        public IReadOnlyList<CharsetInfo> GetAll() => [];

        public CharsetDefinition? GetById(string id) => null;

        public void Save(string id, CharsetDefinition definition) => throw new NotSupportedException();

        public string Create(CharsetDefinition definition) => throw new NotSupportedException();
    }

    [Fact]
    public void Draw_with_illusory_zoom_wraps_increments_segment_index()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(16, 8);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var analysis = new AudioAnalysisSnapshot
        {
            CurrentBpm = 120,
            SmoothedMagnitudes = new double[8],
            TargetMaxMagnitude = 1
        };

        var palette = new List<PaletteColor> { PaletteColor.FromConsoleColor(ConsoleColor.Gray) };
        var layerSettings = new TextLayerSettings { LayerType = TextLayerType.FractalZoom, ColorIndex = 0 };
        layerSettings.SetCustom(new FractalZoomSettings
        {
            IllusoryInfiniteZoom = true,
            ZoomSpeed = 0.02,
            MaxIterations = 8
        });

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = 16,
            ViewportHeight = 8,
            Width = 16,
            Height = 8,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 5.0
        };

        var store = new TextLayerStateStore();
        ITextLayerStateStore<FractalZoomState> fractalStore = store;
        FractalZoomState st = fractalStore.GetState(0);
        st.ZoomPhase = 0.0;

        var layer = new FractalZoomLayer(store, new CharsetResolver(new EmptyCharsetRepo()));
        var scroll = (0.0, 0);
        layer.Draw(layerSettings, ref scroll, ctx);

        Assert.True(st.SegmentIndex >= 5, $"expected several wraps, got SegmentIndex={st.SegmentIndex}");
        Assert.InRange(st.ZoomPhase, 0.0, 1.0);
    }

    [Fact]
    public void Draw_with_illusory_zoom_disabled_resets_segment_and_julia_offsets()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(8, 4);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var analysis = new AudioAnalysisSnapshot
        {
            CurrentBpm = 120,
            SmoothedMagnitudes = new double[4],
            TargetMaxMagnitude = 1
        };

        var palette = new List<PaletteColor> { PaletteColor.FromConsoleColor(ConsoleColor.Gray) };
        var layerSettings = new TextLayerSettings { LayerType = TextLayerType.FractalZoom, ColorIndex = 0 };
        layerSettings.SetCustom(new FractalZoomSettings
        {
            IllusoryInfiniteZoom = false,
            ZoomSpeed = 0.001,
            MaxIterations = 8
        });

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = 8,
            ViewportHeight = 4,
            Width = 8,
            Height = 4,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var store = new TextLayerStateStore();
        ITextLayerStateStore<FractalZoomState> fractalStore = store;
        FractalZoomState st = fractalStore.GetState(0);
        st.SegmentIndex = 12;
        st.JuliaOffsetRe = 0.04;
        st.JuliaOffsetIm = -0.03;

        var layer = new FractalZoomLayer(store, new CharsetResolver(new EmptyCharsetRepo()));
        var scroll = (0.0, 0);
        layer.Draw(layerSettings, ref scroll, ctx);

        Assert.Equal(0, st.SegmentIndex);
        Assert.Equal(0.0, st.JuliaOffsetRe);
        Assert.Equal(0.0, st.JuliaOffsetIm);
    }

    /// <summary>
    /// Contract: <c>specs/text-layers-visualizer/layers/fractal-zoom/spec.md</c> — Scenario: Viewport is not exceeded.
    /// FractalZoom only iterates local <c>x ∈ [0, Width)</c>, <c>y ∈ [0, Height)</c> and writes via <see cref="TextLayerDrawContext.SetLocal"/>;
    /// cells outside the layer-local region (here offset into a larger buffer) must stay unchanged.
    /// </summary>
    [Fact]
    public void Draw_does_not_write_outside_layer_local_region_contract_viewport_is_not_exceeded()
    {
        var marker = PaletteColor.FromRgb(1, 2, 3);
        const char sentinel = '?';

        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(14, 10);
        buffer.Clear(marker);
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                buffer.Set(x, y, sentinel, marker);
            }
        }

        var analysis = new AudioAnalysisSnapshot
        {
            CurrentBpm = 120,
            SmoothedMagnitudes = new double[8],
            TargetMaxMagnitude = 1
        };

        var palette = new List<PaletteColor> { PaletteColor.FromConsoleColor(ConsoleColor.Gray) };
        var layerSettings = new TextLayerSettings { LayerType = TextLayerType.FractalZoom, ColorIndex = 0 };
        layerSettings.SetCustom(new FractalZoomSettings
        {
            IllusoryInfiniteZoom = false,
            ZoomSpeed = 0.0005,
            MaxIterations = 8
        });

        const int originX = 5;
        const int originY = 2;
        const int localW = 4;
        const int localH = 3;

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = buffer.Width,
            ViewportHeight = buffer.Height,
            Width = localW,
            Height = localH,
            BufferOriginX = originX,
            BufferOriginY = originY,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var store = new TextLayerStateStore();
        var layer = new FractalZoomLayer(store, new CharsetResolver(new EmptyCharsetRepo()));
        var scroll = (0.0, 0);
        layer.Draw(layerSettings, ref scroll, ctx);

        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                bool inside = x >= originX && x < originX + localW && y >= originY && y < originY + localH;
                (char c, PaletteColor col) = buffer.Get(x, y);
                if (inside)
                {
                    Assert.NotEqual(sentinel, c);
                }
                else
                {
                    Assert.Equal(sentinel, c);
                    Assert.Equal(marker, col);
                }
            }
        }
    }

    /// <summary>
    /// With Julia mode and illusory wraps, <see cref="FractalZoomLayer"/> applies <see cref="FractalZoomIllusoryReseed.JuliaNudgeForSegment"/>
    /// to the user constant so the sampled Julia parameter changes across segments (not only orbit anchor).
    /// </summary>
    [Fact]
    public void Draw_julia_mode_with_illusory_wrap_changes_effective_julia_constant_across_segments()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(4, 4);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var analysis = new AudioAnalysisSnapshot
        {
            CurrentBpm = 120,
            SmoothedMagnitudes = new double[8],
            TargetMaxMagnitude = 1
        };

        var palette = new List<PaletteColor> { PaletteColor.FromConsoleColor(ConsoleColor.Gray) };
        const double userJuliaRe = -0.8;
        const double userJuliaIm = 0.156;
        var layerSettings = new TextLayerSettings { LayerType = TextLayerType.FractalZoom, ColorIndex = 0 };
        layerSettings.SetCustom(new FractalZoomSettings
        {
            FractalMode = FractalZoomMode.Julia,
            JuliaRe = userJuliaRe,
            JuliaIm = userJuliaIm,
            IllusoryInfiniteZoom = true,
            ZoomSpeed = 0.02,
            MaxIterations = 12
        });

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = 4,
            ViewportHeight = 4,
            Width = 4,
            Height = 4,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 5.0
        };

        var store = new TextLayerStateStore();
        ITextLayerStateStore<FractalZoomState> fractalStore = store;
        FractalZoomState st = fractalStore.GetState(0);
        st.ZoomPhase = 0.0;
        st.SegmentIndex = 0;

        var layer = new FractalZoomLayer(store, new CharsetResolver(new EmptyCharsetRepo()));
        var scroll = (0.0, 0);
        layer.Draw(layerSettings, ref scroll, ctx);

        Assert.True(st.SegmentIndex >= 1, $"expected at least one wrap, SegmentIndex={st.SegmentIndex}");
        (double nudgeRe, double nudgeIm) = FractalZoomIllusoryReseed.JuliaNudgeForSegment(st.SegmentIndex);
        Assert.Equal(nudgeRe, st.JuliaOffsetRe, precision: 10);
        Assert.Equal(nudgeIm, st.JuliaOffsetIm, precision: 10);

        double jRe = userJuliaRe + st.JuliaOffsetRe;
        double jIm = userJuliaIm + st.JuliaOffsetIm;
        bool anySampleDiffers = false;
        for (int iy = -8; iy <= 8 && !anySampleDiffers; iy++)
        {
            for (int ix = -8; ix <= 8 && !anySampleDiffers; ix++)
            {
                double cr = ix * 0.07;
                double ci = iy * 0.07;
                double withNudge = FractalZoomSampler.EscapeSmoothJulia(cr, ci, jRe, jIm, 64);
                double withoutNudge = FractalZoomSampler.EscapeSmoothJulia(cr, ci, userJuliaRe, userJuliaIm, 64);
                if (Math.Abs(withNudge - withoutNudge) > 1e-9)
                {
                    anySampleDiffers = true;
                }
            }
        }

        Assert.True(anySampleDiffers, "Julia nudge from illusory segment should change EscapeSmoothJulia at some z in the search box.");
    }
}
