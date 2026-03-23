# ADR-0060: Universal title breadcrumb on all surfaces

**Status**: Accepted

## Context

The title bar breadcrumb (ADR-0036) was only visible in the main header. Overlay and full-screen modals replace or clear the console without drawing the same path, so users lose orientation (preset, mode, and sub-view). We need a **preset-scoped** path (`app/mode/preset/...`) for editor-related modals and an **app-settings** path (`app/settings/...`) for global settings (e.g. audio input) and a future settings hub. Sub-views (e.g. palette picker inside S) append further segments.

## Decision

1. **Two tracks**:
   - **Preset-scoped**: `appName/mode/preset` plus optional suffixes (`/showedit`, `/help`). The main view shows `[z]:layerName` after the preset (ADR-0036). The **S (preset settings) modal** uses the **same** `preset[n]:layerType` pattern as main for the **focused layer** (no `/settings`). When focus is on the settings column, a **`/hackerizedSettingId`** segment reflects the highlighted row (e.g. `Palette`, `Speed`). When the **palette picker** is open, **`/editor`** is appended after that segment.
   - **App-settings**: `appName/settings/...` (e.g. `/audioinput` for device selection). No mode or preset segment. Intended for **device selection** today and a future **ApplicationMode.Settings** hub.

2. **Navigation context**: A singleton **`ITitleBarNavigationContext`** (implementation in Console) holds **`TitleBarViewKind`**, **`PresetSettingsPalettePickerActive`**, **`PresetSettingsLayerOneBased`**, **`PresetSettingsLayerTypeRaw`**, and **`PresetSettingsFocusedSettingId`** (highlighted setting row id in the S modal). Modals set **`View`** on open and reset to **`Main`** on close; the settings modal renderer updates navigation fields each draw from **`SettingsModalState`** and the sorted layer list.

3. **No duplicate modal titles**: Framed “title bar” boxes (preset name, HELP, Show name, device picker) were removed; **row 0** breadcrumb carries that context instead.

4. **Single formatter**: **`ITitleBarBreadcrumbFormatter`** builds one ANSI breadcrumb line (same cyberpunk palette rules as ADR-0036). **`ITitleBarContentProvider`** delegates to it when **`View == Main`**. Modals call the formatter after updating navigation context.

5. **Row 0 everywhere**: The breadcrumb occupies **terminal row 0** on every surface. **Overlay modals** use **`OverlayRowCount`** for the covered top rows (settings/show overlays were tightened after removing inner title frames). **Full-screen modals** draw the breadcrumb on row 0 and content below.

6. **Hackerize**: Path segments (including `settings` on the app-settings track, setting ids, `editor`, `audioinput`, `help`, `showedit`) use **`TextHelpers.Hackerize`** for stylistic consistency with ADR-0036.

7. **`ApplicationMode.Settings`**: When **`View == Main`** and **`ApplicationMode == Settings`**, the breadcrumb is **`appName/settings`** only (hub home). Sub-routes (e.g. `/audioinput`) use the app-settings track via **`TitleBarViewKind`** (e.g. device modal). Hub UI and Tab cycle: [ADR-0061](0061-general-settings-mode.md). Persisted mode follows ADR-0029.

## Consequences

- All modals that hide the main header must draw the breadcrumb or call a shared helper so the path stays consistent.
- **`VisualizationOrchestrator`** behavior unchanged: overlay still suppresses header refresh; **row 0** is owned by the modal draw path.
- **Tests**: `ITitleBarBreadcrumbFormatter` is unit-tested with fixed `UiSettings` / `VisualizerSettings` and a stub `IVisualizer`.
- Supplements [ADR-0036](0036-title-bar-injectable-component.md) (injectable title bar); does not replace the main `{app}/{mode}/{preset}[z]:layer` format for the default view.

## References

- [TitleBarBreadcrumbFormatter](../../src/AudioAnalyzer.Application/Display/TitleBarBreadcrumbFormatter.cs)
- [ITitleBarNavigationContext](../../src/AudioAnalyzer.Application/Abstractions/ITitleBarNavigationContext.cs)
- [TitleBarNavigationContext](../../src/AudioAnalyzer.Console/Console/TitleBarNavigationContext.cs)
