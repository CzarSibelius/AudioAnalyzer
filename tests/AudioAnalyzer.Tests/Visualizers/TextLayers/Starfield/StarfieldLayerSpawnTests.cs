using AudioAnalyzer.Application;
using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Application.Viewports;
using AudioAnalyzer.Domain;
using AudioAnalyzer.Visualizers;
using Xunit;

namespace AudioAnalyzer.Tests.Visualizers.TextLayers.Starfield;

/// <summary>Spawn determinism for <see cref="StarfieldLayer"/> with fixed RNG seed.</summary>
public sealed class StarfieldLayerSpawnTests
{
    private sealed class FakeCharsetRepo : ICharsetRepository
    {
        public IReadOnlyList<CharsetInfo> GetAll() => Array.Empty<CharsetInfo>();
        public CharsetDefinition? GetById(string id) => null;
        public string Create(CharsetDefinition definition) => throw new NotSupportedException();
        public void Save(string id, CharsetDefinition definition) => throw new NotSupportedException();
    }

    [Fact]
    public void Draw_with_fixed_seed_reproduces_frame_after_full_state_reset()
    {
        var store = new TextLayerStateStore();
        var resolver = new CharsetResolver(new FakeCharsetRepo());
        var layer = new StarfieldLayer(store, resolver);

        var buffer = new ViewportCellBuffer();
        buffer.EnsureSize(32, 16);
        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));

        var layerSettings = new TextLayerSettings
        {
            LayerType = TextLayerType.Starfield,
            SpeedMultiplier = 0.01,
            ColorIndex = 0
        };
        layerSettings.SetCustom(new StarfieldSettings
        {
            StarCount = 80,
            BaseSpeed = 0.02,
            FixedRandomSeed = 424242,
            ZNear = 0.2,
            ZFar = 50,
            FocalLength = 28,
            SpreadX = 2.5,
            SpreadY = 2.5,
            DepthShading = StarfieldDepthShading.Flat
        });

        var palette = new List<PaletteColor> { PaletteColor.FromRgb(200, 200, 200) };

        var ctx = new TextLayerDrawContext
        {
            Buffer = buffer,
            Frame = new VisualizationFrameContext { Analysis = new AudioAnalysisSnapshot() },
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

        var tuple = (0.0, 0);
        layer.Draw(layerSettings, ref tuple, ctx);

        int markX = -1, markY = -1;
        char markCh = ' ';
        for (int y = 0; y < 16 && markX < 0; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                var (ch, _) = buffer.Get(x, y);
                if (ch != ' ')
                {
                    markX = x;
                    markY = y;
                    markCh = ch;
                    break;
                }
            }
        }

        Assert.True(markX >= 0, "Expected at least one star glyph in buffer.");

        buffer.Clear(PaletteColor.FromRgb(0, 0, 0));
        StarfieldLayerState st = ((ITextLayerStateStore<StarfieldLayerState>)store).GetState(0);
        st.LastWidth = -1;
        st.LastHeight = -1;
        st.LastStarCount = -1;
        st.LastSpawnFixedSeed = int.MinValue;

        layer.Draw(layerSettings, ref tuple, ctx);
        var (ch2, _) = buffer.Get(markX, markY);
        Assert.Equal(markCh, ch2);
    }
}
