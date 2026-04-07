# ADR-0071: UI themes as separate JSON files from layer palettes

**Status**: Accepted

## Context

Layer palettes (`palettes/*.json`) are indexed color lists for visualizers. Using the same files as the **UI theme** tied chrome colors to full palette lists and showed every palette in the General Settings theme picker, which is noisy and semantically wrong. Users need **first-class UI themes** that store only semantic UI and title-bar slots, optionally referencing a layer palette for base mapping and animated swatches.

## Decision

1. **Storage**: UI themes live in **`themes/*.json`** next to the executable (same discovery pattern as `presets/`). Id = filename without extension.

2. **Persistence**: `UiSettings.UiThemeId` (optional string) replaces `UiSettings.UiThemePaletteId`. There is **no migration** of the old property; users who upgrade should back up `appsettings.json` and set `UiThemeId` again ([ADR-0029](0029-no-settings-migration.md)).

3. **Theme file schema** (`UiThemeDefinition`):
   - **`Name`** (optional): display name in lists.
   - **`FallbackPaletteId`** (optional): layer palette id. When it resolves, **base** `UiPalette` and `TitleBarPalette` are built with existing **`UiThemePaletteMapper`** (indices 0–4 UI, 5–10 title bar). When missing or invalid, base comes from inline `UiSettings.Palette` and `UiSettings.TitleBarPalette` (title bar defaults when null).
   - **`Ui`** / **`TitleBar`** (optional nested objects): explicit `PaletteColorEntry` per semantic slot; **overlay** replaces base slots when present.

4. **Resolution**: `IUiThemeResolver` loads the theme via **`IUiThemeRepository`**, computes base with **`UiThemeMerger.ResolveBase`**, then **`UiThemeMerger.MergeOverlay`**. If `UiThemeId` is unset or the theme file is missing/invalid, behavior matches previous **(Custom)** inline path.

5. **General Settings hub**: **UI theme (T)** opens **`IUiThemeSelectionModal`**. The list is **`(Custom)`** + **theme files only** (not layer palettes). **N** starts **new theme from palette**: pick a source palette, then assign each of 11 slots to a palette color index (←/→), **Enter** on **Save new theme** writes a new file (via `IUiThemeRepository.Create`) and selects it.

6. **Hub swatch animation**: When the active theme has `FallbackPaletteId`, hub value coloring uses that palette’s color list; otherwise it uses semantic `UiPalette` slots (same as Custom).

## Consequences

- Layer palettes remain independent of UI theme selection; **`P`** and preset palette rows are unchanged.
- Ship at least one default theme under `themes/` (e.g. `default.json` with `FallbackPaletteId` only) so the picker is useful out of the box.
- Tests use `ThemesDirectory` / mock file system like shows; `IUiThemeRepository` is injectable in `ServiceConfigurationOptions`.

## References

- Supersedes [ADR-0065](0065-ui-theme-from-layer-palettes.md) for UI theme storage and picker behavior (layer palette id on `UiSettings`).
