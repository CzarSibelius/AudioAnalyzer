# ADR-0065: UI theme from shared layer palette files

**Status**: Accepted

## Context

Users need a single place to tune **UI chrome colors** (header, toolbar labels, modals) and **title bar breadcrumb colors** without hand-editing many slots. TextLayers already use **JSON palette files** in the `palettes/` directory (same ids as `IPaletteRepository`). Reusing those palettes for UI keeps visual consistency and avoids a second palette system.

## Decision

1. **Persisted field**: `UiSettings.UiThemePaletteId` (optional string, same id semantics as `TextLayersVisualizerSettings.PaletteId` — filename without extension). Stored under `UiSettings` in `appsettings.json`.

2. **Precedence**: When `UiThemePaletteId` is **set** and resolves to a non-empty color list via `IPaletteRepository` + `ColorPaletteParser`, **effective** `UiPalette` and `TitleBarPalette` are **derived** from that file. When it is **null/empty**, or the id cannot be resolved, the app uses **inline** `UiSettings.Palette` and `UiSettings.TitleBarPalette` from JSON (title bar falls back to domain defaults when `TitleBarPalette` is null), unchanged from prior behavior.

3. **Index mapping** (deterministic): For parsed colors `colors[0..K-1]`, `K >= 1`, define `At(i) = colors[i % K]`.
   - **UiPalette**: `Normal=At(0)`, `Highlighted=At(1)`, `Dimmed=At(2)`, `Label=At(3)`, `Background=At(4)`.
   - **TitleBarPalette**: `AppName=At(5)`, `Mode=At(6)`, `Preset=At(7)`, `Layer=At(8)`, `Separator=At(9)`, `Frame=At(10)`.

4. **Resolution**: `IUiThemeResolver` is the single API for effective UI and title bar palettes; title bar formatting and all chrome that used raw `UiSettings.Palette` must use it.

5. **General Settings hub**: Third menu row **UI theme (T)** opens injectable **`IUiThemeSelectionModal`** (blocking, same modal pattern as device selection). First list entry **`(Custom)`** clears `UiThemePaletteId` and saves; other entries set it to the chosen palette id. **Enter** on the row or **T** when that row is selected opens the modal.

## Consequences

- **No auto-sync** with the active preset’s `TextLayers.PaletteId`; users pick the same palette id manually if they want a match.
- **No migration** of old files; new property is optional ([ADR-0029](0029-no-settings-migration.md)).
- **Help and bindings**: General settings hub exposes the **T** binding for the theme row via `IKeyHandler` entries.
- **Tests**: Mapper and resolver have unit tests; palette list drawing reuses shared `SettingsSurfacesPaletteDrawing.FormatPickerPaletteRow` patterns ([ADR-0063](0063-uniform-settings-list-and-palette-drawing.md)).
