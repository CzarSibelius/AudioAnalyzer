# ADR-0024: AnalysisSnapshot as frame context

**Status**: Accepted

## Context

A design review of `AnalysisSnapshot` found: (1) the docstring claimed it was "produced by the engine" but `Palette` and `CurrentPaletteName` were set by the renderer; (2) `Palette` was never read (TextLayers resolves palettes via `IPaletteRepository` and `ResolvePaletteForLayer`); (3) the snapshot acted as a frame context bag with two producers, not strictly engine output.

## Decision

1. **AnalysisSnapshot is a frame context**, not strictly engine output. The engine fills analysis data (FFT, waveform, volume, beats, layout); the renderer may add optional display data (e.g. `CurrentPaletteName` for toolbar).

2. **Remove `Palette` from the snapshot**. It was never consumed; visualizers resolve palettes themselves (e.g. TextLayers via `IPaletteRepository` and `ResolvePaletteForLayer` per layer).

3. **Keep `CurrentPaletteName`** in the snapshot for toolbar display. The renderer sets it when the visualizer supports palette cycling; the toolbar reads it for the palette name line.

4. **Keep `BeatSensitivity`** in the snapshot for toolbar display. It is an engine setting, not analysis output, but putting it in the snapshot avoids coupling the toolbar to the engine directly.

## Consequences

- Snapshot docstring updated to describe "frame context" with two producers.
- `Palette` property removed from [AnalysisSnapshot.cs](../../src/AudioAnalyzer.Application/Abstractions/AnalysisSnapshot.cs).
- [VisualizationPaneLayout](../../src/AudioAnalyzer.Infrastructure/VisualizationPaneLayout.cs) sets only `CurrentPaletteName`, not `Palette`.
- ADR-0002 and ADR-0003 described snapshot carrying palette colors; that design is superseded by per-visualizer resolution (TextLayers uses `IPaletteRepository`; no consumer ever used `snapshot.Palette`).
