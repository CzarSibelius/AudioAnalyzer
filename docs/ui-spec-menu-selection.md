# UI spec: Menu and list selection affordance

Canonical rules for **keyboard-selected rows** in the console UI: hub menus, full-screen lists, overlay columns, and palette pickers. **ADR:** [ADR-0069](adr/0069-unified-menu-selection-affordance.md). **Implementation:** `MenuSelectionAffordance`, `SettingsSurfacesListDrawing`, `SettingsSurfacesPaletteDrawing`, plus call sites below.

## Rules

1. **Prefix:** Selected row begins with **` ► `** (space, U+25BA BLACK RIGHT-POINTING POINTER, space). Non-selected rows use **`   `** (three spaces) so columns align.
2. **Highlight:** Selected rows use **background + foreground** across the **full target width** of the row (content + padding to the column or modal line width). Trailing padding must sit **inside** the selection ANSI span so the bar reads as one block (see `MenuSelectionAffordance.FormatAnsiSelectableRow`).
3. **Colors:** **`UiPalette.Background`** with fallback **`ConsoleColor.DarkBlue`** for the selection background; **`UiPalette.Highlighted`** for the selection foreground. Resolve via **`IUiThemeResolver.GetEffectiveUiPalette()`** (or the same effective palette supplied in **`RenderContext.Palette`** for General settings).
4. **Swatch / palette names:** Rows may keep **PaletteSwatchFormatter** per-grapheme colors inside the selection band; the arrow and row padding still use the unified selection block.

## Exceptions

- **Non-selectable** static lines (hints, separators, titles) do not use this affordance.
- **General hub — selected row:** While selected, the value uses the same solid selection foreground as the label (no beat/tick **PaletteSwatchFormatter** animation on that row) so the highlight matches other surfaces.

## Inventory (surface → code)

| Surface | Selection drawing |
|---------|-------------------|
| General settings hub menu | `GeneralSettingsHubMenuLines` + `MenuSelectionAffordance.FormatAnsiSelectableRow` when selected; `GeneralSettingsHubAreaRenderer` passes row width. |
| Device selection modal list | `SettingsSurfacesListDrawing.DrawAudioDeviceList` |
| UI theme palette modal list | `SettingsSurfacesListDrawing.DrawUiThemePaletteList` |
| S modal — left column (Preset / layers) | `SettingsModalRenderer` |
| S modal — settings column (incl. Palette / Default palette rows) | `SettingsModalRenderer` + `SettingsSurfacesPaletteDrawing.FormatPaletteSettingRow` / `FormatPresetDefaultPaletteSettingRow` |
| S modal — palette picker | `SettingsSurfacesPaletteDrawing.DrawPicker` / `FormatPickerPaletteRow` |
| Show edit modal — entry list | `ShowEditModal` |

## Related specs

- [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md) — hub, device, S modal overview.
- [ADR-0063](adr/0063-uniform-settings-list-and-palette-drawing.md) — where shared list/palette helpers live (plus ADR-0069 for appearance).
