using AudioAnalyzer.Application.Abstractions;
using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Visualizers;

/// <summary>Builds default TextLayers presets and padding layers using typed layer custom settings.</summary>
public sealed class DefaultTextLayersSettingsFactory : IDefaultTextLayersSettingsFactory
{
    /// <inheritdoc />
    public TextLayersVisualizerSettings CreateDefault()
    {
        var layers = new List<TextLayerSettings>
        {
            CreateLayer(TextLayerType.GeissBackground, 0, ["Layered text"], speed: 1.0, geissBeat: GeissBackgroundBeatReaction.Flash),
            new() { LayerType = TextLayerType.BeatCircles, ZOrder = 1, SpeedMultiplier = 1.0 },
            CreateLayer(TextLayerType.Marquee, 2, ["Layered text", "Audio visualizer"], speed: 1.0, marqueeBeat: MarqueeBeatReaction.SpeedBurst),
            CreateLayer(TextLayerType.Marquee, 3, ["Layer 3"], speed: 0.8, marqueeBeat: MarqueeBeatReaction.None),
            CreateLayer(TextLayerType.WaveText, 4, ["Wave"], speed: 1.0, waveTextBeat: WaveTextBeatReaction.Pulse),
            CreateLayer(TextLayerType.StaticText, 5, ["Static"], staticTextBeat: StaticTextBeatReaction.None),
            CreateLayer(TextLayerType.FallingLetters, 6, [".*#%"], speed: 1.0, fallingLettersBeat: FallingLettersBeatReaction.SpawnMore),
            CreateLayer(TextLayerType.MatrixRain, 7, [], speed: 1.0, matrixRainBeat: MatrixRainBeatReaction.Flash),
            CreateLayer(TextLayerType.StaticText, 8, ["Top"], staticTextBeat: StaticTextBeatReaction.Pulse)
        };
        return new TextLayersVisualizerSettings { Layers = layers };
    }

    /// <inheritdoc />
    public TextLayerSettings CreatePaddingMarqueeLayer(int zOrder, int displayLayerNumber)
    {
        var padLayer = new TextLayerSettings
        {
            LayerType = TextLayerType.Marquee,
            ZOrder = zOrder,
            TextSnippets = [$"Layer {displayLayerNumber}"],
            SpeedMultiplier = 1.0
        };
        padLayer.SetCustom(new MarqueeSettings { BeatReaction = MarqueeBeatReaction.None });
        return padLayer;
    }

    private static TextLayerSettings CreateLayer(TextLayerType layerType, int zOrder, List<string> textSnippets, double speed = 1.0,
        GeissBackgroundBeatReaction? geissBeat = null, MarqueeBeatReaction? marqueeBeat = null, WaveTextBeatReaction? waveTextBeat = null,
        StaticTextBeatReaction? staticTextBeat = null, FallingLettersBeatReaction? fallingLettersBeat = null, MatrixRainBeatReaction? matrixRainBeat = null)
    {
        var layer = new TextLayerSettings
        {
            LayerType = layerType,
            ZOrder = zOrder,
            TextSnippets = textSnippets,
            SpeedMultiplier = speed
        };
        if (geissBeat.HasValue)
        {
            layer.SetCustom(new GeissBackgroundSettings { BeatReaction = geissBeat.Value });
        }
        if (marqueeBeat.HasValue)
        {
            layer.SetCustom(new MarqueeSettings { BeatReaction = marqueeBeat.Value });
        }
        if (waveTextBeat.HasValue)
        {
            layer.SetCustom(new WaveTextSettings { BeatReaction = waveTextBeat.Value });
        }
        if (staticTextBeat.HasValue)
        {
            layer.SetCustom(new StaticTextSettings { BeatReaction = staticTextBeat.Value });
        }
        if (fallingLettersBeat.HasValue)
        {
            layer.SetCustom(new FallingLettersSettings { BeatReaction = fallingLettersBeat.Value });
        }
        if (matrixRainBeat.HasValue)
        {
            layer.SetCustom(new MatrixRainSettings { BeatReaction = matrixRainBeat.Value });
        }
        return layer;
    }
}
