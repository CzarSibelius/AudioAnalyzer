# UI spec: Preset / layer settings modal (S)

Overlay for editing the active **preset** (rename, new) and **text layer** settings. Open with **S** in Preset editor or Show play ([ADR-0023](adr/0023-settings-modal-layer-editing.md)). Not available in General settings ([ADR-0061](adr/0061-general-settings-mode.md)). See [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md) for shared patterns.

## Overlay geometry

- **Overlay row count:** 16 rows (`OverlayRowCount` in `SettingsModal`).
- **Row 0:** Title breadcrumb ([ADR-0060](adr/0060-universal-title-breadcrumb.md)); path includes preset name, `[n]:layerType`, optional focused setting id, optional `/palette` when the palette picker is open.
- **Row 1:** Hint line (single **HorizontalRowComponent**).
- **Row 2:** Horizontal separator (`  ─…─┬─…─`).
- **Rows 3–15:** Content area: **left column** (fixed width ~28) layer list; **right column** remainder — either settings rows, **palette picker** list, or (when renaming preset) relevant UI. Visible settings rows count = overlay rows minus first content row (13 visible lines for settings/picker).

Minimum console width ~40 columns; below that the modal may skip drawing.

## Screenshot

```text
aUdioNLZR/pReset/pReset_2[1]:aScii_image
  1-9 select, ←→ type, Ctrl+↑↓ move, Enter settings, Shift+1-9 toggle, R rename, N preset, Esc close
  ────────────────────────────┬───────────────────────────────────
 ► ● 1. Ascii_image            Enabled:Yes
   ○ 2. Fill                   ZOrder:2
```

*(Representative; regenerate from a screen dump when verifying.)*

## Line reference

**1** — Title breadcrumb: preset-scoped track with layer index and type when applicable.

**2** — Hint text depends on **`SettingsModalFocus`** and rename state (see below).

**3** — Separator between left (layer list) and right (settings) columns.

**4–16** — Layer list (left): selection prefix ` ► ` or `   `; enabled **●** / disabled **○**; `n. LayerType`. Right: settings `Label:value`, text-edit buffer with cursor, or palette picker sub-UI.

## Focus modes (`SettingsModalFocus`)

| Focus | Hint summary (see `GetHintText` in `SettingsModalRenderer`) |
|-------|----------------------------------------------------------------|
| **LayerList** | Layer shortcuts: 1–9, arrows, Enter → settings, Shift+1–9 toggle, R rename, N new preset, Esc close. |
| **Renaming** | Type preset name; Enter save; Esc cancel. |
| **SettingsList** | Move in settings; Enter cycle or open palette list on Palette; string rows Enter to edit; ←/Esc back to layer list. |
| **EditingSetting** | Type value; Enter or ↑↓ confirm; Esc cancel. |
| **PickingPalette** | Right column shows scrollable list: **(inherit)** plus repository palettes; ↑↓ or +/- preview; Enter save; Esc discard. Palette names use **PaletteSwatchFormatter** coloring; idle updates match toolbar phase (`DrawIdleOverlayTick`). |

State fields: `SettingsModalState` (`SelectedLayerIndex`, `SelectedSettingIndex`, `PalettePickerSelectedIndex`, `EditingBuffer`, `RenameBuffer`, etc.).

## Related code

- Renderer: `SettingsModalRenderer` — hint via **IUiComponent** tree; list/palette list via shared `PalettePickerListDrawer` / `SettingsSurfacesListDrawing` ([ADR-0063](adr/0063-uniform-settings-list-and-palette-drawing.md)).
- Keys: `SettingsModalKeyHandler`.
