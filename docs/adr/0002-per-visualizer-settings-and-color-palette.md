# ADR-0002: Per-visualizer settings and reusable color palette

**Status**: Accepted

## Context

We need to add visualizers that have their own configuration (e.g. the Unknown Pleasures visualizer with a configurable color palette). Adding one top-level property to `AppSettings` per visualizer would not scale and would mix global and visualizer-specific concerns. We also want a reusable structure for color palettes so multiple visualizers can use named color sets.

## Decision

1. **Per-visualizer settings**: Each visualizer that needs configuration has its **own settings type** (e.g. `UnknownPleasuresVisualizerSettings`, `GeissVisualizerSettings`, `OscilloscopeVisualizerSettings`). These types live in the **Visualizers** project, each in its visualizer subfolder (e.g. `TextLayers/TextLayerSettings.cs`, `Geiss/GeissVisualizerSettings.cs`). `VisualizerSettings` aggregates per-visualizer properties and lives in Visualizers. `AppSettings` (Domain) holds only app-level config; it does **not** reference `VisualizerSettings` to avoid circular dependencies (see [ADR-0010](0010-appsettings-visualizer-settings-separation.md)).

2. **Reusable color palette**: A **`ColorPalette`** type (Domain) holds `Name` (optional) and `ColorNames` (ordered array of console color names). It is serialization-friendly and does not reference `ConsoleColor`. Parsing from `ColorPalette` to `IReadOnlyList<ConsoleColor>` is done in Application/Infrastructure (e.g. `ColorPaletteParser`). Visualizers that need a palette receive the resolved colors via the snapshot (e.g. `AnalysisSnapshot.UnknownPleasuresPalette`) so they remain stateless; the renderer fills the snapshot from the current visualizer settings before calling `Render`.

3. **Backward compatibility**: When loading settings, if `VisualizerSettings` or a visualizer’s section is missing, merge from legacy top-level properties (e.g. `BeatCircles`, `OscilloscopeGain`) so existing config files continue to work. When saving, persist the effective values into `VisualizerSettings` so the file migrates to the new structure.

## Consequences

- **Domain**: `ColorPalette`, `AppSettings` (app-level only; legacy BeatCircles/OscilloscopeGain for migration). Per-visualizer settings types live in Visualizers.
- **Application**: Parsing helper for palette → `ConsoleColor`; snapshot may carry mode-specific data (e.g. `UnknownPleasuresPalette`) filled by the renderer.
- **Infrastructure**: Renderer caches resolved palette (or other visualizer options) from settings and sets the snapshot before calling the corresponding visualizer. Settings repository merges legacy keys into `VisualizerSettings` on load.
- **Console**: After loading settings, resolve per-visualizer values (e.g. palette from `VisualizerSettings.UnknownPleasures.Palette`) and pass them to the renderer/engine. When saving, write back into `VisualizerSettings`.
- **Documentation**: README and ADR index updated to describe the per-visualizer structure and palette configuration.
