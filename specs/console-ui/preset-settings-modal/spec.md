# UI spec: Preset / layer settings modal (S)

## Blueprint

### Context

Console UI surface documented with ASCII screen dumps and line references per [format](../format/spec.md) and [ADR-0046](../../../docs/adr/0046-screen-dump-ascii-screenshot.md).

### Architecture

Overlay for editing **preset-level** settings (name, default palette), **preset** shortcuts (rename **R**, new **N**, **delete entire preset**), and **text layer** settings (including **Insert** / **Delete** to add or remove layers up to the max count; [ADR-0070](../../../docs/adr/0070-settings-modal-add-remove-layers.md)). Open with **S** in Preset editor or Show play ([ADR-0023](../../../docs/adr/0023-settings-modal-layer-editing.md)). Not available in General settings ([ADR-0061](../../../docs/adr/0061-general-settings-mode.md)). See [ui-spec-settings-surfaces.md](../settings-surfaces/spec.md) for shared patterns. Selectable rows (left list, right settings, palette picker): [ui-spec-menu-selection.md](../menu-selection/spec.md). **L** opens the same **layer type picker** as on the main Preset editor canvas when **LayerList** focus allows (full contract: [preset-editor-navigation/spec.md](../preset-editor-navigation/spec.md)); **Ins** / **Del** remain the modal’s primary add/remove affordance for **layers** while **S** is open. **Preset-level delete:** when the left-column **Preset** row is selected (same focus as **R** / **N**), plain **Delete** removes the **whole** active preset (disk file via [`IPresetRepository.Delete`](../../../src/AudioAnalyzer.Application/Abstractions/IPresetRepository.cs)), not a text layer — see Constraints and Contract below. Persistence model: [ADR-0022](../../../docs/adr/0022-presets-in-own-files.md).

**Initial layer selection:** When the modal opens, the left-column **layer** highlight matches the active layer in the main Preset editor (the same target as **1–9** and the toolbar), not always the first layer.

## Overlay geometry

- **Overlay row count:** 16 rows (`OverlayRowCount` in `SettingsModal`).
- **Row 0:** Title breadcrumb ([ADR-0060](../../../docs/adr/0060-universal-title-breadcrumb.md)); path includes preset name; optional `[n]:layerType` when a **layer** line is selected; optional focused setting id; optional `/editor` when the palette picker is open. When the **Preset** line is selected, the breadcrumb omits `[n]:layerType` but may still show the focused preset setting id (e.g. Name, DefaultPalette).
- **Row 1:** Hint line (single **HorizontalRowComponent**).
- **Row 2:** Horizontal separator (  `─…─┬─…─`).
- **Rows 3–15 (0-based):** Content area: **left column** (fixed width ~28) — row 3 is **Preset** ( `► Preset` when selected); rows 4+ are up to nine **layer** lines when present ( `► ● n. LayerType` /    `○ …`); a preset may have zero layers. When `**UiSettings.ShowLayerRenderTime`** is on ([ADR-0073](../../../docs/adr/0073-layer-render-time-overlay.md)), each layer line may append a compact timing suffix (milliseconds or em dash); the full line is ellipsis-truncated to the left column width. The suffix is colored from the effective UI palette (**Highlighted** / **Normal** / **Dimmed**) by comparing measured time to a fair-share budget: **60 FPS frame time** divided by **enabled** layer count (over budget → Highlighted; mid → Normal; low or untimed em dash → Dimmed). The **selected** layer row uses plain timing text so the menu selection highlight stays uniform. **Right column** (settings, palette picker, rename) starts on **row 4** (`RightColumnContentStartRow` in `SettingsModalRenderer`) so the first setting line lines up with **1.** on the left, not with the **Preset** header; row 3 right is blank. When **Preset** is selected, **Name** / **Default palette** occupy the first visible right rows in that aligned region; when a **layer** is selected, that layer’s settings (reflection-driven); or **palette picker** (inherit + repo for layer **Palette**; repo-only for preset **Default palette**); or (when renaming preset via **R**) hint-only rename. Visible settings/picker row count = `OverlayRowCount - RightColumnContentStartRow` (12 lines).

Minimum console width ~40 columns; below that the modal may skip drawing.

## Screenshot

```text
aUdioNLZR/pReset/pReset_2[1]:aScii_image
  1-9 layer, Ins add, Del remove layer or delete preset, ↑↓ Preset or layers, ←→ type, Ctrl+↑↓ move, Enter settings, Shift+1-9 toggle, R rename, N preset, Esc close
  ────────────────────────────┬───────────────────────────────────
 ► Preset
   ● 1. Ascii_image            Name:My preset
   ○ 2. Marquee                Default palette:…
```

*(Representative; regenerate from a screen dump when verifying.)*

## Line reference

**1** — Title breadcrumb: preset-scoped track with layer index and type when applicable.

**2** — Hint text depends on `**SettingsModalFocus*`* and rename state (see below).

**3** — Separator between left (layer list) and right (settings) columns.

**4** — Left: **Preset** row (no ●/○). Right: blank (spacer so settings align with layer lines).

**5–16** — Left: layer list with selection prefix `►` or    ``; enabled **●** / disabled **○**; `n. LayerType`; optional tier-colored per-layer render-time suffix when **Show layer render time** is enabled (see geometry note above). Right: preset **Name** / **Default palette**, or layer settings — each selectable row uses the same `**►` /    `*`* prefix and full-width UI theme highlight as other menus ([ui-spec-menu-selection.md](../menu-selection/spec.md)); **Palette** / **Default palette** rows include the arrow and block highlight with swatch-colored names. Palette picker sub-UI matches device/theme list affordance (first right line on the same row as **1.**).

## Focus modes (`SettingsModalFocus`)


| Focus              | Hint summary (see `GetHintText` in `SettingsModalRenderer`)                                                                                                                                                                                                               |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **LayerList**      | **1–9** selects a layer; **Insert** adds a layer (up to max); **Delete** removes the **selected layer** when a **layer** row is selected, or **deletes the entire active preset** when the **Preset** row is selected (see Contract); **L** opens the layer type picker for the selected layer row (nested overlay); **↑/↓** moves between **Preset** and layers; **Enter** opens right column (preset or layer settings); Shift+1–9 toggle; **R** rename; **N** new preset; Esc close.   |
| **Renaming**       | Type preset name; Enter save; Esc cancel.                                                                                                                                                                                                                                 |
| **SettingsList**   | Move in settings; Enter cycle or open palette list on Palette; string rows Enter to edit; ←/Esc back to layer list.                                                                                                                                                       |
| **EditingSetting** | Type value; Enter or ↑↓ confirm; Esc cancel.                                                                                                                                                                                                                              |
| **PickingPalette** | Layer **Palette** row: scrollable **(inherit)** plus repository palettes. Preset **Default palette** row: repository palettes only (no inherit). ↑↓ or +/- preview; Enter save; Esc discard. **PaletteSwatchFormatter** coloring; idle updates via `DrawIdleOverlayTick`. |


State fields: `SettingsModalState` (`LeftPanelPresetSelected`, `SelectedLayerIndex`, `PalettePickerForPresetDefault`, `SelectedSettingIndex`, `PalettePickerSelectedIndex`, `EditingBuffer`, `RenameBuffer`, etc.).

## Related code

- Renderer: `SettingsModalRenderer` — hint via **IUiComponent** tree; list/palette list via `SettingsSurfacesPaletteDrawing` / `SettingsSurfacesListDrawing` ([ADR-0063](../../../docs/adr/0063-uniform-settings-list-and-palette-drawing.md)); preset rows from `PresetSettingsModalRows`.
- Keys: `SettingsModalKeyHandler` (`SettingsModalKeyHandlerConfig`).

### Constraints

- **8-column blocks** and **Label:value** formatting per [ADR-0050](../../../docs/adr/0050-ui-alignment-blocks-label-format.md).
- **Delete disambiguation:** Plain **Delete** in **LayerList** depends on selection: **layer** row → remove that layer ([ADR-0070](../../../docs/adr/0070-settings-modal-add-remove-layers.md)); **Preset** row → delete the **active preset** file and switch the app to another preset (below). Same key as canvas layer delete when **S** is closed ([preset-editor-navigation/spec.md](../preset-editor-navigation/spec.md)); no separate global shortcut for preset delete unless product adds one later.
- **At least one preset:** [ADR-0019](../../../docs/adr/0019-preset-textlayers-configuration.md) requires a non-empty preset collection. If only **one** preset exists, **Delete** on the **Preset** row is a **no-op** (preset file is not removed; `ActivePresetId` unchanged).
- **After a successful preset delete:** Compute the successor **`ActivePresetId`** from the preset list **before** removing the file (same result as **V** from the soon-to-be-deleted preset): [`PresetNavigationOrder.GetNextPresetIdByDisplayName`](../../../src/AudioAnalyzer.Domain/PresetNavigationOrder.cs)(`presetsBeforeDelete`, `activeId`) — **not** from `GetAll()` after `Delete`, which would mis-order when the deleted id is no longer in the list. Then call `IPresetRepository.Delete(activeId)`; refresh `VisualizerSettings.Presets` from `GetAll()`; assign the computed successor id; load `TextLayers` from the new active preset’s stored config; persist settings; notify layer structure / state so the visualizer matches the new preset (same invariants as switching presets via **V**).
- **Show play:** Same **S** modal rules when Show play opens the preset / layer overlay (not General settings).
- Regenerate screenshot + **Line reference** when layout or semantics change.

## Contract

### Definition of Done

- Screenshot block matches a fresh screen dump when rows or labels change.
- Every screen line in the dump has a matching **Line reference** entry.
- Hint line documents **Del** for both layer removal and preset deletion where applicable; [`SettingsModalRenderer`](../../../src/AudioAnalyzer.Console/SettingsModal/SettingsModalRenderer.cs) hint text matches this spec after behavior ships.
- **Delete** on **Preset** row with **two or more** presets removes the active preset file, activates the successor per **Constraints**, and leaves the modal in a consistent state (left list, `TextLayers`, persistence).
- **Delete** on **Preset** row with **one** preset has no effect on disk or active id.

### Regression guardrails

- Cross-links to other console-ui specs and ADRs resolve after moves under specs/console-ui/.
- **Delete** on a **layer** row still only removes layers (max/min counts, state store) per ADR-0070; preset delete must not regress layer-delete paths.
- Help (**H**) and settings-modal binding lists describe preset delete only where **S** / **LayerList** bindings are listed.

### Scenarios

```gherkin
Scenario: Capture matches spec
  Given the documented mode is active in a Windows console
  When the operator triggers a screen dump (Ctrl+Shift+E per ADR-0046)
  Then pasted ASCII matches the spec screenshot block line-for-line for controlled fixtures

Scenario: Delete on Preset row removes entire preset when another exists
  Given Preset editor or Show play is active and the S preset settings overlay is open
  And at least two presets exist
  And the left-column Preset row is selected
  When the user presses Delete
  Then the former active preset file is removed via the preset repository
  And the active preset becomes the next in ascending display-name order with wrap
  And TextLayers match the newly active preset's saved configuration

Scenario: Delete on Preset row is ignored when only one preset exists
  Given the S modal is open with the Preset row selected
  And exactly one preset exists
  When the user presses Delete
  Then no preset file is deleted
  And ActivePresetId is unchanged

Scenario: Delete on layer row still removes only the layer
  Given the S modal is open with a layer row selected
  When the user presses Delete
  Then only that layer is removed per ADR-0070
  And the preset file for the active preset remains
```
