# ADR-0028: Layer dependency injection

**Status**: Accepted

## Context

Data flowed through the render pipeline via `AnalysisSnapshot` even when it originated from external services. For example, `CurrentNowPlayingText` was fetched by `VisualizationPaneLayout` from `INowPlayingProvider`, stuffed into the snapshot, and consumed only by the NowPlaying layer. This created unnecessary coupling: the renderer had to know about now-playing to pass it through, and the snapshot carried service-derived data alongside frame context (analysis data). Layers that need services should receive them via constructor injection instead of via the snapshot.

## Decision

1. **Layers with service dependencies receive them via constructor injection**. Example: NowPlayingLayer receives `INowPlayingProvider` and calls `GetNowPlaying()` in Draw; it no longer reads from the snapshot.

2. **AnalysisSnapshot does not carry service-derived data**. The snapshot remains frame context: engine output (FFT, waveform, volume, beats, layout). Toolbar/UI display data (e.g. palette name) is read from the renderer’s own state, not from the snapshot. Data from long-lived services (INowPlayingProvider, future lyrics API, etc.) is not passed through the snapshot.

3. **Layers are registered in the DI container and resolved via `IEnumerable<ITextLayerRenderer>`**. All `ITextLayerRenderer` implementations are registered in ServiceConfiguration (via `AddTextLayerRenderers()`). TextLayersVisualizer receives `IEnumerable<ITextLayerRenderer>`, builds a dictionary by `LayerType`, and no longer constructs layers manually. Layers that need services (e.g. NowPlayingLayer with `INowPlayingProvider`) get them via constructor injection when resolved from the container.

4. **Adding new layers**: Create the layer class in the Visualizers assembly; it is discovered and registered via reflection in `AddTextLayerRenderers()`. No edits to ServiceCollectionExtensions or TextLayersVisualizer required.

## Consequences

- NowPlayingLayer injects `INowPlayingProvider`; `CurrentNowPlayingText` removed from AnalysisSnapshot.
- VisualizationPaneLayout no longer receives or uses `INowPlayingProvider`.
- TextLayersVisualizer receives `IEnumerable<ITextLayerRenderer>` and builds a dictionary; it no longer needs `INowPlayingProvider` or `CreateRenderers()`.
- ServiceConfiguration: `AddTextLayerRenderers()` registers all layers; TextLayersVisualizer gets `IEnumerable<ITextLayerRenderer>`; INowPlayingProvider is resolved by NowPlayingLayer when created by the container.
- References: [NowPlayingLayer](../../src/AudioAnalyzer.Visualizers/TextLayers/NowPlaying/NowPlayingLayer.cs), [ADR-0024](0024-analysissnapshot-frame-context.md).
