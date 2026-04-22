using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.MandalaRingPulse;

public sealed class MandalaRingPulseLayerTests
{
    [Fact]
    public void Draw_writes_ring_cells_for_small_viewport()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(32, 16);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var analysis = new AudioAnalysisSnapshot
        {
            CurrentBpm = 120,
            SmoothedMagnitudes = new double[16],
            TargetMaxMagnitude = 1
        };

        var palette = new List<PaletteColor> { PaletteColor.FromConsoleColor(ConsoleColor.White) };
        var layerSettings = new TextLayerSettings
        {
            LayerType = TextLayerType.MandalaRingPulse,
            ColorIndex = 0
        };
        layerSettings.SetCustom(new MandalaRingPulseSettings
        {
            Pattern = MandalaRingPulsePattern.ConcentricRings,
            RingCount = 5,
            PulsesPerBeat = 2,
            PulseDepth = 0.25,
            EnergyMix = 0.5
        });

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = 32,
            ViewportHeight = 16,
            Width = 32,
            Height = 16,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var store = new TextLayerStateStore();
        var layer = new MandalaRingPulseLayer(store);
        var scroll = (0.0, 0);
        layer.Draw(layerSettings, ref scroll, ctx);

        int hits = 0;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                var (ch, _) = buffer.Get(x, y);
                if (ch is '·' or '░' or '▒')
                {
                    hits++;
                }
            }
        }

        Assert.True(hits > 40);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(20)]
    [InlineData(301)]
    public void Draw_invalid_bpm_freezes_tempo_phase_between_frames(double currentBpm)
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(24, 12);
        var black = PaletteColor.FromRgb(0, 0, 0);
        buffer.Clear(black);

        var analysis = new AudioAnalysisSnapshot
        {
            CurrentBpm = currentBpm,
            SmoothedMagnitudes = new double[8],
            TargetMaxMagnitude = 1
        };

        var palette = new List<PaletteColor> { PaletteColor.FromConsoleColor(ConsoleColor.White) };
        var layerSettings = new TextLayerSettings
        {
            LayerType = TextLayerType.MandalaRingPulse,
            ColorIndex = 0
        };
        layerSettings.SetCustom(new MandalaRingPulseSettings
        {
            Pattern = MandalaRingPulsePattern.ConcentricRings,
            RingCount = 5,
            PulsesPerBeat = 4,
            PulseDepth = 0.25,
            AngularMotion = 0,
            EnergyMix = 0
        });

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = 24,
            ViewportHeight = 12,
            Width = 24,
            Height = 12,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var store = new TextLayerStateStore();
        var layer = new MandalaRingPulseLayer(store);
        var scroll = (0.0, 0);

        layer.Draw(layerSettings, ref scroll, ctx);
        string first = DumpChars(buffer);

        buffer.Clear(black);
        layer.Draw(layerSettings, ref scroll, ctx);
        string second = DumpChars(buffer);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Draw_valid_bpm_advances_tempo_phase_between_frames()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(24, 12);
        var black = PaletteColor.FromRgb(0, 0, 0);
        buffer.Clear(black);

        var analysis = new AudioAnalysisSnapshot
        {
            CurrentBpm = 120,
            SmoothedMagnitudes = new double[8],
            TargetMaxMagnitude = 1
        };

        var palette = new List<PaletteColor> { PaletteColor.FromConsoleColor(ConsoleColor.White) };
        var layerSettings = new TextLayerSettings
        {
            LayerType = TextLayerType.MandalaRingPulse,
            ColorIndex = 0
        };
        layerSettings.SetCustom(new MandalaRingPulseSettings
        {
            Pattern = MandalaRingPulsePattern.ConcentricRings,
            RingCount = 5,
            PulsesPerBeat = 4,
            PulseDepth = 0.25,
            AngularMotion = 0,
            EnergyMix = 0
        });

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = 24,
            ViewportHeight = 12,
            Width = 24,
            Height = 12,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 1,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var store = new TextLayerStateStore();
        var layer = new MandalaRingPulseLayer(store);
        var scroll = (0.0, 0);

        layer.Draw(layerSettings, ref scroll, ctx);
        string first = DumpChars(buffer);

        buffer.Clear(black);
        layer.Draw(layerSettings, ref scroll, ctx);
        string second = DumpChars(buffer);

        Assert.NotEqual(first, second);
    }

    private static string DumpChars(ViewportCellBuffer buffer)
    {
        var sb = new System.Text.StringBuilder(buffer.Width * buffer.Height);
        for (int y = 0; y < buffer.Height; y++)
        {
            for (int x = 0; x < buffer.Width; x++)
            {
                var (ch, _) = buffer.Get(x, y);
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }
}
