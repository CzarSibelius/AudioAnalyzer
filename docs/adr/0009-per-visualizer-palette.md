# ADR-0009: Per-visualizer palette selection

**Status**: Accepted

## Context

Previously, `AppSettings.SelectedPaletteId` stored a single palette for all palette-aware visualizers (Geiss, Unknown Pleasures, Layered text). When the user pressed P to cycle palettes, the change applied globally. Users may want different palettes for different visualizers—e.g. one palette for Geiss and another for Unknown Pleasures—and expect each to persist separately when switching modes.

## Decision

1. **Per-visualizer PaletteId**: Each palette-aware visualizer has its own `PaletteId` property in its settings type (`GeissVisualizerSettings`, `UnknownPleasuresVisualizerSettings`, `TextLayersVisualizerSettings`). The palette is resolved from `IPaletteRepository` by id at startup for each mode.

2. **P key behavior**: When the user presses P, only the **current** visualizer's palette cycles. The new palette id is stored in that visualizer's settings and persisted on save. If the current mode does not support palette cycling, P has no effect.

3. **Renderer**: The composite renderer stores palettes per mode (`SetPaletteForMode`) and passes the appropriate palette for the active mode into the snapshot when rendering.

4. **Backward compatibility**: On load, if a palette-aware visualizer's `PaletteId` is null/empty and `AppSettings.SelectedPaletteId` is set, the migration copies `SelectedPaletteId` to that visualizer's `PaletteId`. For Unknown Pleasures, the legacy `ColorPalette? Palette` remains as a fallback when neither `PaletteId` nor `SelectedPaletteId` provides a value.

## Consequences

- **Domain**: `PaletteId` added to `GeissVisualizerSettings`, `UnknownPleasuresVisualizerSettings`, `TextLayersVisualizerSettings`. `SelectedPaletteId` in `AppSettings` is deprecated for palette selection but retained for migration.
- **Application/Infrastructure**: `IVisualizationRenderer.SetPalette` replaced with `SetPaletteForMode(mode, palette, displayName)`; composite renderer uses a per-mode palette dictionary.
- **Console**: Startup resolves and sets palette for each palette-aware mode; `CyclePalette` guards on current mode, updates that visualizer's `PaletteId`, and calls `SetPaletteForMode` for the current mode only.
- **TextLayers (later amendment)**: Each layer has its own `PaletteId` in `TextLayerSettings`. P cycles the focused layer (last selected with 1–9). `TextLayers.PaletteId` is the fallback when a layer has no `PaletteId`. TextLayers does not use `SetPaletteForMode`; it resolves palettes per-layer at render time via `IPaletteRepository`.
- **Settings repository**: `MergeLegacyVisualizerSettings` migrates `SelectedPaletteId` to each visualizer's `PaletteId` when empty; ensures `UnknownPleasures` is initialized.
- **Documentation**: README and visualizer specs updated to describe per-visualizer palettes.
