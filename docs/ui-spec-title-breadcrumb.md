# UI spec: Universal title breadcrumb (row 0)

This document describes the **title breadcrumb line** that appears on **row 0** of the terminal for the main header and every modal (full-screen or overlay). See [ADR-0060](../adr/0060-universal-title-breadcrumb.md) and [ADR-0036](../adr/0036-title-bar-injectable-component.md).

## Rules

- **Row 0** is reserved for the breadcrumb on: main view (header), settings modal (S), show edit modal, help modal (H), and device selection modal.
- **Preset-scoped** surfaces use: `appName` / `mode` / `preset` / … where `mode` is hackerized `Preset` or `Show`, and `preset` is the active preset name (hackerized). The **main** view adds `[z]:layer` after the preset (no slash before `[z]`). The **S modal** uses the same `preset[z]:layerType` prefix for the selected layer, then optional `/hackerizedSettingId` when the settings column is focused, then optional `/editor` when the palette picker is open. Other preset modals (show edit, help) append a single hackerized suffix after `preset`.
- **App-settings** surfaces use: `appName` / `settings` / … (no mode or preset). Device selection uses `/audioinput` as the third segment.
- Segments use **`TextHelpers.Hackerize`** (same style as the main title bar). Separators `/` use the dim separator color from `TitleBarPalette`.

## Path examples (plain text, without ANSI)

| Surface | Example path (illustrative) |
|--------|-----------------------------|
| Main (Preset editor / Show play) | `aUdioNLZR/pReset/my_Preset[3]:fIll` |
| S modal (layer list focus) | `aUdioNLZR/pReset/my_Preset[3]:fIll` |
| S modal (Preset row, settings column — no `[n]:layer`) | `aUdioNLZR/pReset/my_Preset/dEfaultPalette` |
| S modal (settings column, e.g. Speed) | `aUdioNLZR/pReset/my_Preset[3]:fIll/sPeed` |
| S modal + palette picker | `aUdioNLZR/pReset/my_Preset[3]:fIll/pAlette/eDitor` |
| S modal Preset row + palette picker | `aUdioNLZR/pReset/my_Preset/dEfaultPalette/eDitor` |
| Show edit modal | `aUdioNLZR/sHow/my_Preset/sHowedit` |
| Help modal | `aUdioNLZR/pReset/my_Preset/hElp` |
| Device (audio input) | `aUdioNLZR/sEttings/aUdioinput` |
| Future: `ApplicationMode.Settings` hub home | `aUdioNLZR/sEttings` |

## Screenshot

Regenerate from a screen dump (`--dump-after N`) when verifying; strip ANSI for readability. The first line of any screen should match the pattern above for the active surface.

```text
(line 1 — title breadcrumb only; left-aligned, ellipsis if wider than terminal)
```

## Line reference

- **1** — Title breadcrumb: single-line path as above; `IDisplayText` as preformatted ANSI from `ITitleBarBreadcrumbFormatter` / `ITitleBarContentProvider`.
