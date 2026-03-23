# ADR-0063: Uniform settings list and palette drawing

**Status**: Accepted

## Context

General settings hub, device selection modal, and the preset (S) settings modal offered similar interactions (vertical lists, palette rows, palette picker) but duplicated drawing logic in separate types ([ADR-0053](0053-iuicomponent-all-ui.md) migration is incremental).

## Decision

1. **Shared list scroll math** — `SettingsSurfacesListDrawing.ComputeListScrollOffset` is the single implementation for keeping a selected index visible in a fixed-height window (device list and palette picker).

2. **Device list drawing** — `SettingsSurfacesListDrawing.DrawAudioDeviceList` draws the full-screen device selection list; `DeviceSelectionModal` delegates to it.

3. **Palette picker and Palette setting row** — `SettingsSurfacesPaletteDrawing` owns picker list drawing (`DrawPicker`), picker row ANSI (`FormatPickerPaletteRow`), and the Palette setting cell (`FormatPaletteSettingRow`). `SettingsModalRenderer` delegates to these helpers.

4. **General settings hub menu rows** — Hub `Label:value` lines are built by `GeneralSettingsHubMenuLines` and rendered via **HorizontalRowComponent** + **IUiComponentRenderer&lt;HorizontalRowComponent&gt;** so the same row primitive as toolbar/hint rows applies ([ADR-0057](0057-horizontal-row-unified-single-line-rows.md)).

## Consequences

- Changes to list appearance or scroll behavior should be made in the shared helpers so device and palette surfaces stay aligned.
- Further modal migration to full **IUiComponent** trees can wrap or replace these helpers without changing key-handling contracts.
- UI specs: [ui-spec-settings-surfaces.md](../ui-spec-settings-surfaces.md).
