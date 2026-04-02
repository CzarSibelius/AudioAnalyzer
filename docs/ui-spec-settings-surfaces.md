# UI spec: Settings surfaces (shared patterns)

Top-level **application modes** (full-screen layouts) are documented separately: [ui-spec-application-modes.md](ui-spec-application-modes.md).

This document ties together **General settings hub**, **device selection modal**, and **preset / layer settings modal (S)** so specs and implementation share the same vocabulary. Per-surface dumps and line-by-line references live in:

- [ui-spec-general-settings-hub.md](ui-spec-general-settings-hub.md)
- [ui-spec-device-selection-modal.md](ui-spec-device-selection-modal.md)
- [ui-spec-preset-settings-modal.md](ui-spec-preset-settings-modal.md)

Layout rules: [ui-spec-format.md](ui-spec-format.md), [ADR-0050](adr/0050-ui-alignment-blocks-label-format.md).

## When each surface appears

| Surface | How to open | ADR |
|--------|----------------|-----|
| **General settings hub** | **Tab** to General settings mode (after Preset editor / Show play when eligible). | [ADR-0061](adr/0061-general-settings-mode.md), [ADR-0062](adr/0062-application-mode-classes.md) |
| **Device selection modal** | **D** from main loop (Preset / Show), or **Enter** on **Audio input devices** in the General hub. | [ADR-0035](adr/0035-modal-dependency-injection.md) |
| **Preset / layer settings (S)** | **S** in Preset editor or Show play (not in General settings). | [ADR-0023](adr/0023-settings-modal-layer-editing.md) |

## Universal row 0: title breadcrumb

All surfaces use **ITitleBarBreadcrumbFormatter** and **ITitleBarNavigationContext** on row 0. Rules: [ui-spec-title-breadcrumb.md](ui-spec-title-breadcrumb.md), [ADR-0060](adr/0060-universal-title-breadcrumb.md).

## Shared building blocks

| Building block | Role |
|----------------|------|
| **Hint line** | Short instructions (toolbar-style). In the S modal, the hint row is rendered via **HorizontalRowComponent** ([ADR-0057](adr/0057-horizontal-row-unified-single-line-rows.md)). |
| **Label:value** | [ADR-0050](adr/0050-ui-alignment-blocks-label-format.md): colon immediately after the label, no space before the value; no hotkey suffix in labels ([ADR-0034](adr/0034-viewport-label-hotkey-hints.md) superseded). |
| **Menu rows (hub)** | Compact settings list with optional selection prefix; hub uses the same label/value idea as modal hints where applicable. |
| **Vertical selectable list** | **↑/↓** move selection, **Enter** confirms, **Esc** cancels (device modal; palette picker in S modal uses **Esc** to discard preview). |
| **Palette name coloring** | **PaletteSwatchFormatter** — beat- or tick-driven phase for per-letter colors (toolbar, S modal Palette row, palette picker list). |

## Selection affordances (current variants)

Until implementation unifies, specs document **both**:

- **Hub menu rows:** leading `>` on the selected row, two spaces otherwise (`GeneralSettingsHubAreaRenderer`).
- **Modal lists (device, S modal layer list):** leading ` ► ` on the selected row, three spaces otherwise.

Palette picker rows in the S modal use background/foreground highlight on the selected entry (including **(inherit)**).

## Feature matrix (where capabilities exist today)

| Feature | General hub | Device modal | S modal (preset / layers) |
|--------|-------------|--------------|---------------------------|
| Title breadcrumb row | Yes | Yes | Yes |
| Hint / instruction line | Toolbar row | Line below breadcrumb | Line 1 (HorizontalRowComponent) |
| `Label:value` menu | Yes (audio, app name) | No (plain list) | Settings column |
| Inline text edit | Application name | No | Preset rename, string settings |
| Full-screen list picker | No | Yes | Palette picker (right column) |
| Two-column overlay | No | No | Yes (layers \| settings / picker) |
| Palette swatch / animation | Toolbar palette cell | No | Palette row + picker list |
| Idle redraw (palette phase) | Same as main toolbar | No | Yes (`DrawIdleOverlayTick`) |

**Global palette:** In General settings, palette is shown on the **toolbar** and cycled from the main key map; the hub does not host a palette list picker (same as toolbar-only cycling unless product direction changes).

## Related overlay: Show edit modal

**Show edit** (**S** in Show play) uses a similar **two-column overlay** shell as the S modal (breadcrumb, hint, separator, list \| detail). It does not yet have a dedicated ui-spec; when added, reuse the “overlay shell” structure described in [ui-spec-preset-settings-modal.md](ui-spec-preset-settings-modal.md).

## Implementation note

Shared list and palette drawing for device selection and the S modal palette picker are consolidated in the Console project (`SettingsSurfacesListDrawing`, `PalettePickerListDrawer`) per [ADR-0063](adr/0063-uniform-settings-list-and-palette-drawing.md).
