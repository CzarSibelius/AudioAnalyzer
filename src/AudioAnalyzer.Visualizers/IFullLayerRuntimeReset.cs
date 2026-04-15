namespace AudioAnalyzer.Visualizers;

/// <summary>Resets in-memory per-layer draw state on the text-layers visualizer (offsets, snippet indices).</summary>
public interface IFullLayerRuntimeReset
{
    /// <summary>Clears scroll/marquee offsets and snippet indices for all layer slots; does not change preset JSON.</summary>
    void ResetAllRuntimeState();
}
