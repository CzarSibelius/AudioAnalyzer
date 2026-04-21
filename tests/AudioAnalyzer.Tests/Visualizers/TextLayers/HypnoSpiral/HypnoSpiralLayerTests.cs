using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.HypnoSpiral;

public sealed class HypnoSpiralLayerTests
{
    private sealed class EmptyCharsetRepo : ICharsetRepository
    {
        public IReadOnlyList<CharsetInfo> GetAll() => [];

        public CharsetDefinition? GetById(string id) => null;

        public void Save(string id, CharsetDefinition definition) => throw new NotSupportedException();

        public string Create(CharsetDefinition definition) => throw new NotSupportedException();
    }

    [Fact]
    public void Draw_advances_twist_each_frame()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(24, 12);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var analysis = new AudioAnalysisSnapshot
        {
            CurrentBpm = 120,
            SmoothedMagnitudes = Array.Empty<double>(),
            TargetMaxMagnitude = 1
        };

        var palette = new List<PaletteColor> { PaletteColor.FromConsoleColor(ConsoleColor.Gray) };
        var layerSettings = new TextLayerSettings { LayerType = TextLayerType.HypnoSpiral, ColorIndex = 0 };
        layerSettings.SetCustom(new HypnoSpiralSettings
        {
            ArmCount = 8,
            LogPitch = 8,
            RevolutionsPerBeat = 1,
            MoireMix = 0.5
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
        ITextLayerStateStore<HypnoSpiralState> typed = store;
        double before = typed.GetState(0).TwistRadians;

        var layer = new HypnoSpiralLayer(store, new CharsetResolver(new EmptyCharsetRepo()));
        var scroll = (0.0, 0);
        layer.Draw(layerSettings, ref scroll, ctx);
        layer.Draw(layerSettings, ref scroll, ctx);

        double after = typed.GetState(0).TwistRadians;
        Assert.True(after > before);
    }

    [Fact]
    public void Draw_negative_color_index_does_not_throw_with_palette()
    {
        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(8, 6);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var analysis = new AudioAnalysisSnapshot
        {
            CurrentBpm = 120,
            SmoothedMagnitudes = Array.Empty<double>(),
            TargetMaxMagnitude = 1
        };

        var palette = new List<PaletteColor>
        {
            PaletteColor.FromConsoleColor(ConsoleColor.Red),
            PaletteColor.FromConsoleColor(ConsoleColor.Blue)
        };

        var layerSettings = new TextLayerSettings
        {
            LayerType = TextLayerType.HypnoSpiral,
            ColorIndex = -5
        };

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = analysis },
            Palette = palette,
            SpeedBurst = 1.0,
            ViewportWidth = 8,
            ViewportHeight = 6,
            Width = 8,
            Height = 6,
            BufferOriginX = 0,
            BufferOriginY = 0,
            LayerIndex = 0,
            FrameDeltaSeconds = 1.0 / 60.0
        };

        var store = new TextLayerStateStore();
        var layer = new HypnoSpiralLayer(store, new CharsetResolver(new EmptyCharsetRepo()));
        var scroll = (0.0, 0);
        var ex = Record.Exception(() => layer.Draw(layerSettings, ref scroll, ctx));
        Assert.Null(ex);
    }
}
