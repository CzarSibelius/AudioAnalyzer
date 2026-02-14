# ADR-0004 compliance audit

This document records how the codebase aligns with [ADR-0004: Visualizer encapsulation](0004-visualizer-encapsulation.md).

## Post-refactor summary (current state)

After the refactor described in "Recommended direction" below:

| Area | Status | Notes |
|------|--------|--------|
| Renderer: concrete visualizer reference | **OK** | No concrete type held; all visualizers accessed via `IVisualizer` |
| Renderer: visualizer-specific API | **OK** | `SetShowBeatCircles` / `GetShowBeatCircles` removed; palette is `SetPalette` on interface |
| Renderer: toolbar branch on mode | **OK** | Toolbar uses `IVisualizer.GetToolbarSuffix(snapshot)`; Oscilloscope returns gain line |
| Renderer: palette method name | **OK** | `SetPalette` on `IVisualizationRenderer`; snapshot `Palette` |
| Console: concrete renderer + visualizer-specific calls | **OK** | Resolves only `IVisualizationRenderer`; beat circles/gain via engine |
| Application (engine): capability-based property names | **OK** | `WaveformGain`, `ShowBeatCircles`; snapshot filled by engine |
| Snapshot: capability-based property names | **OK** | `Palette`, `WaveformGain`, `ShowBeatCircles` |
| Settings / repository | **OK** | Per-visualizer settings (ADR-0002); persistence only |
| Help text (mode descriptions) | **OK** | Uses interface; enum switch is documentation only |

---

## Original audit (pre-refactor)

The following sections described the state **before** the refactor. They are kept for context.

### Original summary table

| Area | Status | Notes |
|------|--------|--------|
| Renderer: concrete visualizer reference | **Violation** | `_geissVisualizer` held for beat circles |
| Renderer: visualizer-specific API | **Violation** | `SetShowBeatCircles` / `GetShowBeatCircles` |
| Renderer: toolbar branch on mode | **Violation** | `mode == VisualizationMode.Oscilloscope` for Gain line |
| Renderer: palette method name | **Partial** | `SetUnknownPleasuresPalette` is visualizer-specific naming |
| Console: concrete renderer + visualizer-specific calls | **Violation** | Uses `VisualizationPaneLayout` and Geiss/Oscilloscope/palette APIs |
| Application (engine): Oscilloscope-named property | **Violation** | `OscilloscopeGain` on engine and snapshot |
| Snapshot: visualizer-specific property names | **Partial** | `UnknownPleasuresPalette`, `OscilloscopeGain` |
| Settings / repository | **OK** | Per-visualizer settings (ADR-0002); no logic tied to internals |
| Help text (mode descriptions) | **OK** | Uses interface (`GetDisplayName`, `SupportsPaletteCycling`); enum switch is documentation only |

---

## 1. Renderer: concrete visualizer reference and behavior

**ADR**: "The renderer … must not reference concrete visualizer types (e.g. `GeissVisualizer`) for behavior that could be expressed via the shared interface or via the snapshot."

**Current code** ([VisualizationPaneLayout.cs](../../src/AudioAnalyzer.Infrastructure/VisualizationPaneLayout.cs)):

- Holds `private readonly GeissVisualizer _geissVisualizer` and uses it in the mode dictionary and for `SetShowBeatCircles` / `GetShowBeatCircles`.
- Those methods directly get/set `_geissVisualizer.ShowBeatCircles`.

**Gap**: The renderer holds a concrete visualizer type to drive a feature (beat circles) that could be driven via the snapshot (e.g. a snapshot property set by the console/engine from settings) so the visualizer only reads it in `Render`.

---

## 2. Renderer: toolbar branch on visualizer identity

**ADR**: "They must not branch on visualizer identity for logic that belongs inside the visualizer."

**Current code** ([VisualizationPaneLayout.cs](../../src/AudioAnalyzer.Infrastructure/VisualizationPaneLayout.cs) – `GetToolbarLine2`):

```csharp
if (mode == VisualizationMode.Oscilloscope)
{
    baseLine = $"Mode: {displayName} (V) | Gain: {snapshot.OscilloscopeGain:F1} ([ ]) | H=Help";
}
```

**Gap**: The renderer knows that "Oscilloscope" has a gain and shows it in the toolbar. This could be made mode-agnostic by e.g. an optional `IVisualizer` member (e.g. "toolbar hint" or "optional gain") or a capability so the renderer does not branch on `VisualizationMode.Oscilloscope`.

---

## 3. Renderer: palette API naming

**Current code**: `SetUnknownPleasuresPalette` and internal `_unknownPleasuresPalette` / `_currentPaletteDisplayName`. Palette is already applied to any visualizer with `SupportsPaletteCycling` (line 70–73), so behavior is capability-based; only the method name is visualizer-specific.

**Gap**: Naming suggests "Unknown Pleasures" only; ADR prefers "mode-agnostic or capability-based naming" (e.g. `SetPalette` / "current palette for palette-cycling modes").

---

## 4. Console: dependency on concrete renderer and visualizer-specific APIs

**ADR**: "Other code must not depend on visualizer internals"; data should flow via "snapshot or viewport" and "shared contract."

**Current code** ([Program.cs](../../src/AudioAnalyzer.Console/Program.cs)):

- Resolves `VisualizationPaneLayout` and calls `SetShowBeatCircles`, `GetShowBeatCircles`, `SetUnknownPleasuresPalette`.
- On load/save: reads/writes `settings.VisualizerSettings.Geiss.BeatCircles`, `settings.VisualizerSettings.Oscilloscope.Gain` and pushes them into the renderer/engine via these methods.
- Key handlers: B toggles beat circles via renderer, [ / ] change gain via engine.

**Gap**: The console knows about Geiss (beat circles), Oscilloscope (gain), and palette (Unknown Pleasures name in API). It should not need to call visualizer-specific methods; settings could be applied to the snapshot (or a shared model) and the renderer/engine would be mode-agnostic.

---

## 5. Application (AnalysisEngine): Oscilloscope-specific property

**Current code** ([AnalysisEngine.cs](../../src/AudioAnalyzer.Application/AnalysisEngine.cs)):

- Public property `OscilloscopeGain` and `_snapshot.OscilloscopeGain` in the build step.

**Gap**: The application layer exposes a visualizer-named concept. ADR prefers capability-based or generic mechanisms (e.g. "waveform gain" or a generic way to pass per-mode options into the snapshot) so the engine does not depend on which visualizer uses gain.

---

## 6. Snapshot: visualizer-specific property names

**Current code** ([AnalysisSnapshot.cs](../../src/AudioAnalyzer.Application/Abstractions/AnalysisSnapshot.cs)):

- `OscilloscopeGain` – named after one visualizer.
- `UnknownPleasuresPalette` – named after one visualizer (though used for any palette-cycling visualizer).

**Gap**: ADR says "Snapshot and settings use mode-agnostic or capability-based naming where feasible." These could be generalized over time (e.g. "WaveformGain", "Palette" / "CurrentPalette") to avoid tying the shared snapshot to specific visualizers.

---

## 7. What already aligns

- **IVisualizer**: Visualizers depend only on `AnalysisSnapshot` and `VisualizerViewport`; no caller needs to know their internals for `Render`.
- **IVisualizationRenderer**: The interface is mode-agnostic (`Render`, `GetDisplayName`, `GetTechnicalName`, `SupportsPaletteCycling`, `GetModeFromTechnicalName`). The violations are on the concrete implementation and callers that use the concrete type.
- **Palette flow**: The renderer sets `snapshot.UnknownPleasuresPalette` for any visualizer with `SupportsPaletteCycling` (capability-based), not only for Unknown Pleasures.
- **Settings schema** (ADR-0002): Per-visualizer settings (Geiss, Oscilloscope, UnknownPleasures) are a separate decision; the repository and schema do not encode visualizer *behavior*, only stored values.
- **Help screen**: Uses `GetDisplayName(mode)` and `SupportsPaletteCycling(mode)`; the per-mode description switch is documentation, not behavioral branching on visualizer identity.

---

## Refactor applied

The following changes were implemented to align with ADR-0004:

1. **Beat circles**: `ShowBeatCircles` added to `AnalysisSnapshot` and `AnalysisEngine`. Engine fills the snapshot; `GeissVisualizer` reads `snapshot.ShowBeatCircles` in `Render`. Renderer no longer holds `_geissVisualizer`; `SetShowBeatCircles` / `GetShowBeatCircles` removed. Console uses `engine.ShowBeatCircles` only.
2. **Gain**: Snapshot and engine use `WaveformGain` (capability-based). `IVisualizer` gained optional `GetToolbarSuffix(snapshot)`; `OscilloscopeVisualizer` returns the gain line; renderer no longer branches on `VisualizationMode.Oscilloscope`.
3. **Palette**: `IVisualizationRenderer.SetPalette`; snapshot property `Palette`; renderer internal `_palette`. `ColorPaletteParser.DefaultPalette` used.
4. **Console**: Resolves only `IVisualizationRenderer` (and `AnalysisEngine`). No `VisualizationPaneLayout` reference; palette via `renderer.SetPalette`, beat circles and gain via engine.
5. **Naming**: Snapshot/engine use `Palette`, `WaveformGain`, `ShowBeatCircles`. Settings file and Domain still use `VisualizerSettings.Geiss.BeatCircles`, `VisualizerSettings.Oscilloscope.Gain`, and legacy `OscilloscopeGain` / `BeatCircles` for backward compatibility.
