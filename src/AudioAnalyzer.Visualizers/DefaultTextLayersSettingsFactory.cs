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
            CreateLayer(TextLayerType.GeissBackground, 0, speed: 1.0, geissBeat: GeissBackgroundBeatReaction.Flash),
            new() { LayerType = TextLayerType.BeatCircles, ZOrder = 1, SpeedMultiplier = 1.0 },
            CreateLayer(TextLayerType.Marquee, 2, ["Layered text", "Audio visualizer"], speed: 1.0, marqueeBeat: MarqueeBeatReaction.SpeedBurst),
            CreateLayer(TextLayerType.Marquee, 3, ["Layer 3"], speed: 0.8, marqueeBeat: MarqueeBeatReaction.None),
            CreateLayer(TextLayerType.WaveText, 4, ["Wave"], speed: 1.0, waveTextBeat: WaveTextBeatReaction.Pulse),
            CreateLayer(TextLayerType.StaticText, 5, ["Static"], staticTextBeat: StaticTextBeatReaction.None),
            CreateFallingLettersLayer(6, FallingLettersAnimationMode.Particles, FallingLettersBeatReaction.SpawnMore, charsetId: null),
            CreateFallingLettersLayer(7, FallingLettersAnimationMode.ColumnRain, FallingLettersBeatReaction.Flash, CharsetIds.Digits),
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
            SpeedMultiplier = 1.0
        };
        padLayer.SetCustom(new MarqueeSettings
        {
            TextSnippets = [$"Layer {displayLayerNumber}"],
            BeatReaction = MarqueeBeatReaction.None
        });
        return padLayer;
    }

    private static TextLayerSettings CreateFallingLettersLayer(
        int zOrder,
        FallingLettersAnimationMode animationMode,
        FallingLettersBeatReaction beat,
        string? charsetId)
    {
        var layer = new TextLayerSettings
        {
            LayerType = TextLayerType.FallingLetters,
            ZOrder = zOrder,
            SpeedMultiplier = 1.0
        };
        layer.SetCustom(new FallingLettersSettings
        {
            AnimationMode = animationMode,
            BeatReaction = beat,
            CharsetId = charsetId
        });
        return layer;
    }

    private static TextLayerSettings CreateLayer(TextLayerType layerType, int zOrder, List<string>? textSnippets = null, double speed = 1.0,
        GeissBackgroundBeatReaction? geissBeat = null, MarqueeBeatReaction? marqueeBeat = null, WaveTextBeatReaction? waveTextBeat = null,
        StaticTextBeatReaction? staticTextBeat = null)
    {
        var layer = new TextLayerSettings
        {
            LayerType = layerType,
            ZOrder = zOrder,
            SpeedMultiplier = speed
        };
        var snippets = textSnippets ?? new List<string>();
        if (geissBeat.HasValue)
        {
            layer.SetCustom(new GeissBackgroundSettings { BeatReaction = geissBeat.Value });
        }
        if (marqueeBeat.HasValue)
        {
            layer.SetCustom(new MarqueeSettings { TextSnippets = snippets, BeatReaction = marqueeBeat.Value });
        }
        if (waveTextBeat.HasValue)
        {
            layer.SetCustom(new WaveTextSettings { TextSnippets = snippets, BeatReaction = waveTextBeat.Value });
        }
        if (staticTextBeat.HasValue)
        {
            layer.SetCustom(new StaticTextSettings { TextSnippets = snippets, BeatReaction = staticTextBeat.Value });
        }
        return layer;
    }
}
