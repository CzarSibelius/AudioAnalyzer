# UI spec: Preset / layer settings modal (S)

Overlay for editing **preset-level** settings (name, default palette), **preset** shortcuts (rename **R**, new **N**), and **text layer** settings. Open with **S** in Preset editor or Show play ([ADR-0023](adr/0023-settings-modal-layer-editing.md)). Not available in General settings ([ADR-0061](adr/0061-general-settings-mode.md)). See [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md) for shared patterns.

**Initial layer selection:** When the modal opens, the left-column **layer** highlight matches the active layer in the main Preset editor (the same target as **1–9** and the toolbar), not always the first layer.

## Overlay geometry

- **Overlay row count:** 16 rows (`OverlayRowCount` in `SettingsModal`).
- **Row 0:** Title breadcrumb ([ADR-0060](adr/0060-universal-title-breadcrumb.md)); path includes preset name; optional `[n]:layerType` when a **layer** line is selected; optional focused setting id; optional `/editor` when the palette picker is open. When the **Preset** line is selected, the breadcrumb omits `[n]:layerType` but may still show the focused preset setting id (e.g. Name, DefaultPalette).
- **Row 1:** Hint line (single **HorizontalRowComponent**).
- **Row 2:** Horizontal separator (`  ─…─┬─…─`).
- **Rows 3–15:** Content area: **left column** (fixed width ~28) — first line **Preset** (` ► Preset` when selected), then up to nine **layer** lines (` ► ● n. LayerType` / `   ○ …`); **right column** — when **Preset** is selected, two rows (**Name**, **Default palette**); when a **layer** is selected, that layer’s settings (reflection-driven); or **palette picker** (inherit + repo for layer **Palette**; repo-only for preset **Default palette**); or (when renaming preset via **R**) hint-only rename. Visible settings rows count = overlay rows minus first content row (13 visible lines for settings/picker).

Minimum console width ~40 columns; below that the modal may skip drawing.

## Screenshot

```text
aUdioNLZR/pReset/pReset_2[1]:aScii_image
  1-9 layer, ↑↓ Preset or layers, ←→ type, Ctrl+↑↓ move, Enter settings, Shift+1-9 toggle, R rename, N preset, Esc close
  ────────────────────────────┬───────────────────────────────────
 ► Preset                      Name:My preset
   ● 1. Ascii_image            Default palette:…
```

*(Representative; regenerate from a screen dump when verifying.)*

## Line reference

**1** — Title breadcrumb: preset-scoped track with layer index and type when applicable.

**2** — Hint text depends on **`SettingsModalFocus`** and rename state (see below).

**3** — Separator between left (layer list) and right (settings) columns.

**4–16** — Left: **Preset** row (no ●/○), then layer list with selection prefix ` ► ` or `   `; enabled **●** / disabled **○**; `n. LayerType`. Right: preset **Name** / **Default palette**, or layer settings `Label:value`, text-edit buffer with cursor, or palette picker sub-UI.

## Focus modes (`SettingsModalFocus`)

| Focus | Hint summary (see `GetHintText` in `SettingsModalRenderer`) |
|-------|----------------------------------------------------------------|
| **LayerList** | **1–9** selects a layer; **↑/↓** moves between **Preset** and layers; **Enter** opens right column (preset or layer settings); Shift+1–9 toggle; **R** rename; **N** new preset; Esc close. |
| **Renaming** | Type preset name; Enter save; Esc cancel. |
| **SettingsList** | Move in settings; Enter cycle or open palette list on Palette; string rows Enter to edit; ←/Esc back to layer list. |
| **EditingSetting** | Type value; Enter or ↑↓ confirm; Esc cancel. |
| **PickingPalette** | Layer **Palette** row: scrollable **(inherit)** plus repository palettes. Preset **Default palette** row: repository palettes only (no inherit). ↑↓ or +/- preview; Enter save; Esc discard. **PaletteSwatchFormatter** coloring; idle updates via `DrawIdleOverlayTick`. |

State fields: `SettingsModalState` (`LeftPanelPresetSelected`, `SelectedLayerIndex`, `PalettePickerForPresetDefault`, `SelectedSettingIndex`, `PalettePickerSelectedIndex`, `EditingBuffer`, `RenameBuffer`, etc.).

## Related code

- Renderer: `SettingsModalRenderer` — hint via **IUiComponent** tree; list/palette list via `SettingsSurfacesPaletteDrawing` / `SettingsSurfacesListDrawing` ([ADR-0063](adr/0063-uniform-settings-list-and-palette-drawing.md)); preset rows from `PresetSettingsModalRows`.
- Keys: `SettingsModalKeyHandler` (`SettingsModalKeyHandlerConfig`).
