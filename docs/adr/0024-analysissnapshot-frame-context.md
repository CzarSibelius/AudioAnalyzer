# ADR-0024: AnalysisSnapshot as frame context

**Status**: Accepted

## Context

A design review of `AnalysisSnapshot` found: (1) the docstring claimed it was "produced by the engine" but `Palette` and `CurrentPaletteName` were set by the renderer; (2) `Palette` was never read (TextLayers resolves palettes via `IPaletteRepository` and `ResolvePaletteForLayer`); (3) the snapshot acted as a frame context bag with two producers, not strictly engine output.

## Decision

1. **AnalysisSnapshot is a frame context**, not strictly engine output. The engine fills analysis data (FFT, waveform, volume, beats, layout).

2. **Remove `Palette` from the snapshot**. It was never consumed; visualizers resolve palettes themselves (e.g. TextLayers via `IPaletteRepository` and `ResolvePaletteForLayer` per layer).

3. **Do not put toolbar/UI display data on the snapshot.** The toolbar (e.g. palette name) reads from the renderer’s own state (e.g. VisualizationPaneLayout’s `_palette.DisplayName`), not from the snapshot.

4. **Keep `BeatSensitivity`** in the snapshot for toolbar display. It is an engine setting, not analysis output, but putting it in the snapshot avoids coupling the toolbar to the engine directly.

5. **Snapshot does not carry service-derived data**. Data from external services (e.g. INowPlayingProvider) should be injected into layers directly, not passed through the snapshot. See [ADR-0028](0028-layer-dependency-injection.md).

## Consequences

- Snapshot docstring updated to describe "frame context" with two producers.
- `Palette` property removed from [AnalysisSnapshot.cs](../../src/AudioAnalyzer.Application/Abstractions/AnalysisSnapshot.cs).
- [VisualizationPaneLayout](../../src/AudioAnalyzer.Console/Console/VisualizationPaneLayout.cs) uses its own `_palette.DisplayName` for the toolbar palette line; the snapshot does not carry palette display data.
- Full-screen and similar UI display state are owned by [IDisplayState](../../src/AudioAnalyzer.Console/Abstractions/IDisplayState.cs) and injected where needed (orchestrator, layout, key handler); they are not on the snapshot or the orchestrator interface.
- ADR-0002 and ADR-0003 described snapshot carrying palette colors; that design is superseded by per-visualizer resolution (TextLayers uses `IPaletteRepository`; no consumer ever used `snapshot.Palette`).
