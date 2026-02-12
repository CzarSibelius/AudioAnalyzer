# ADR-0008: Visualizer settings via Dependency Injection

**Status**: Accepted

## Context

Per-visualizer settings (Oscilloscope gain, Geiss beat circles, TextLayers config, etc.) were previously forwarded through multiple layers: loaded in `Program.cs`, passed to `AnalysisEngine` or `IVisualizationRenderer`, then injected into `AnalysisSnapshot` each frame or stored in the renderer. This caused coupling between the renderer, engine, and snapshot for settings that belong to individual visualizers. The renderer also had to cast to `TextLayersVisualizer` for key handling, violating [ADR-0004](0004-visualizer-encapsulation.md).

## Decision

1. **Constructor injection of per-visualizer settings**: Each visualizer that needs configuration receives only its own settings type via constructor injection (e.g. `OscilloscopeVisualizer(OscilloscopeVisualizerSettings? settings)`). Visualizers without settings keep parameterless constructors.

2. **Settings loaded before renderer construction**: `VisualizerSettings` (and its per-visualizer properties) are loaded from persisted settings before building the dependency injection container. The same instance is registered as a singleton and used by both visualizers and key handlers.

3. **Renderer receives visualizers, not settings**: The renderer receives `IEnumerable<IVisualizer>` and builds its mode-to-visualizer map from the injected instances. It does not hold or forward any per-visualizer settings.

4. **Key handling via IVisualizer**: `IVisualizer` defines `bool HandleKey(ConsoleKey key)` (default: returns false). Visualizers that handle keys (e.g. TextLayers for 1–9) override this and use their injected settings internally. The renderer calls `visualizer.HandleKey(key)` without casting.

5. **Runtime updates**: Key handlers in `Program.cs` update the shared `VisualizerSettings` instance directly (e.g. `visualizerSettings.Oscilloscope.Gain += 0.5`). Because visualizers hold a reference to the same instance, they see changes immediately on the next frame.

## Consequences

- **Visualizers** are self-contained: they receive only what they need (their settings type) and do not depend on snapshot or renderer for configuration.
- **AnalysisSnapshot** no longer carries mode-specific settings (`WaveformGain`, `ShowBeatCircles`, `TextLayersConfig`); it remains mode-agnostic analysis data.
- **AnalysisEngine** no longer holds `WaveformGain` or `ShowBeatCircles`; key handlers update settings directly.
- **IVisualizationRenderer** no longer has `SetTextLayersSettings`; all per-visualizer configuration flows through DI.
- **Encapsulation** is improved: the renderer never references concrete visualizer types for behavior; key handling is fully generic via `IVisualizer.HandleKey`.
- **Bootstrap order** in `Program.cs`: settings are loaded first, then the DI container is configured with `VisualizerSettings` and visualizer registrations, then the renderer and engine are built.
