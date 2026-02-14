namespace AudioAnalyzer.Domain;

/// <summary>How a beat affects a text layer (interpretation is per layer type).</summary>
public enum TextLayerBeatReaction
{
    None,
    SpeedBurst,
    Flash,
    SpawnMore,
    Pulse,
    ColorPop
}
