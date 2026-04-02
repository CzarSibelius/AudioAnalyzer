# UI spec: Preset / layer settings modal (S)

Overlay for editing **preset-level** settings (name, default palette), **preset** shortcuts (rename **R**, new **N**), and **text layer** settings (including **Insert** / **Delete** to add or remove layers up to the max count; [ADR-0070](adr/0070-settings-modal-add-remove-layers.md)). Open with **S** in Preset editor or Show play ([ADR-0023](adr/0023-settings-modal-layer-editing.md)). Not available in General settings ([ADR-0061](adr/0061-general-settings-mode.md)). See [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md) for shared patterns. Selectable rows (left list, right settings, palette picker): [ui-spec-menu-selection.md](ui-spec-menu-selection.md).

**Initial layer selection:** When the modal opens, the left-column **layer** highlight matches the active layer in the main Preset editor (the same target as **1–9** and the toolbar), not always the first layer.

## Overlay geometry

- **Overlay row count:** 16 rows (`OverlayRowCount` in `SettingsModal`).
- **Row 0:** Title breadcrumb ([ADR-0060](adr/0060-universal-title-breadcrumb.md)); path includes preset name; optional `[n]:layerType` when a **layer** line is selected; optional focused setting id; optional `/editor` when the palette picker is open. When the **Preset** line is selected, the breadcrumb omits `[n]:layerType` but may still show the focused preset setting id (e.g. Name, DefaultPalette).
- **Row 1:** Hint line (single **HorizontalRowComponent**).
- **Row 2:** Horizontal separator (`  ─…─┬─…─`).
- **Rows 3–15 (0-based):** Content area: **left column** (fixed width ~28) — row 3 is **Preset** (` ► Preset` when selected); rows 4+ are up to nine **layer** lines when present (` ► ● n. LayerType` / `   ○ …`); a preset may have zero layers. **Right column** (settings, palette picker, rename) starts on **row 4** (`RightColumnContentStartRow` in `SettingsModalRenderer`) so the first setting line lines up with **1.** on the left, not with the **Preset** header; row 3 right is blank. When **Preset** is selected, **Name** / **Default palette** occupy the first visible right rows in that aligned region; when a **layer** is selected, that layer’s settings (reflection-driven); or **palette picker** (inherit + repo for layer **Palette**; repo-only for preset **Default palette**); or (when renaming preset via **R**) hint-only rename. Visible settings/picker row count = `OverlayRowCount - RightColumnContentStartRow` (12 lines).

Minimum console width ~40 columns; below that the modal may skip drawing.

## Screenshot

```text
aUdioNLZR/pReset/pReset_2[1]:aScii_image
  1-9 layer, Ins add, Del remove, ↑↓ Preset or layers, ←→ type, Ctrl+↑↓ move, Enter settings, Shift+1-9 toggle, R rename, N preset, Esc close
  ────────────────────────────┬───────────────────────────────────
 ► Preset
   ● 1. Ascii_image            Name:My preset
   ○ 2. Marquee                Default palette:…
```

*(Representative; regenerate from a screen dump when verifying.)*

## Line reference

**1** — Title breadcrumb: preset-scoped track with layer index and type when applicable.

**2** — Hint text depends on **`SettingsModalFocus`** and rename state (see below).

**3** — Separator between left (layer list) and right (settings) columns.

**4** — Left: **Preset** row (no ●/○). Right: blank (spacer so settings align with layer lines).

**5–16** — Left: layer list with selection prefix ` ► ` or `   `; enabled **●** / disabled **○**; `n. LayerType`. Right: preset **Name** / **Default palette**, or layer settings — each selectable row uses the same **` ► ` / `   `** prefix and full-width UI theme highlight as other menus ([ui-spec-menu-selection.md](ui-spec-menu-selection.md)); **Palette** / **Default palette** rows include the arrow and block highlight with swatch-colored names. Palette picker sub-UI matches device/theme list affordance (first right line on the same row as **1.**).

## Focus modes (`SettingsModalFocus`)

| Focus | Hint summary (see `GetHintText` in `SettingsModalRenderer`) |
|-------|----------------------------------------------------------------|
| **LayerList** | **1–9** selects a layer; **Insert** adds a layer (up to max); **Delete** removes the selected layer; **↑/↓** moves between **Preset** and layers; **Enter** opens right column (preset or layer settings); Shift+1–9 toggle; **R** rename; **N** new preset; Esc close. |
| **Renaming** | Type preset name; Enter save; Esc cancel. |
| **SettingsList** | Move in settings; Enter cycle or open palette list on Palette; string rows Enter to edit; ←/Esc back to layer list. |
| **EditingSetting** | Type value; Enter or ↑↓ confirm; Esc cancel. |
| **PickingPalette** | Layer **Palette** row: scrollable **(inherit)** plus repository palettes. Preset **Default palette** row: repository palettes only (no inherit). ↑↓ or +/- preview; Enter save; Esc discard. **PaletteSwatchFormatter** coloring; idle updates via `DrawIdleOverlayTick`. |

State fields: `SettingsModalState` (`LeftPanelPresetSelected`, `SelectedLayerIndex`, `PalettePickerForPresetDefault`, `SelectedSettingIndex`, `PalettePickerSelectedIndex`, `EditingBuffer`, `RenameBuffer`, etc.).

## Related code

- Renderer: `SettingsModalRenderer` — hint via **IUiComponent** tree; list/palette list via `SettingsSurfacesPaletteDrawing` / `SettingsSurfacesListDrawing` ([ADR-0063](adr/0063-uniform-settings-list-and-palette-drawing.md)); preset rows from `PresetSettingsModalRows`.
- Keys: `SettingsModalKeyHandler` (`SettingsModalKeyHandlerConfig`).
