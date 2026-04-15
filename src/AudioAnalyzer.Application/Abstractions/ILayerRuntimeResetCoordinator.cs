namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Coordinates clearing engine waveform retention and all in-memory text-layer runtime caches (Ctrl+R).</summary>
public interface ILayerRuntimeResetCoordinator
{
    /// <summary>Clears analysis waveform buffers, all shared text-layer state store slots, and visualizer per-layer draw state.</summary>
    void ResetAllLayerRuntimeCaches();
}
