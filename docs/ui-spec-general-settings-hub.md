# UI spec: General Settings hub

This layout applies when **ApplicationMode** is **General settings** (`ApplicationMode.Settings`): **Tab** from Preset editor or Show play (when eligible). The **header is one row** (title breadcrumb only); Device/Now/BPM/Volume rows are not shown ([ADR-0062](adr/0062-application-mode-classes.md)).

Shared vocabulary and feature matrix: [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md). Hub menu rows render via **HorizontalRowComponent** (single full-width preformatted cell) for alignment with other single-line row components ([ADR-0057](adr/0057-horizontal-row-unified-single-line-rows.md)).

## Screenshot

```text
aUdioNLZR/sEttings
General settings — Tab: mode  Up/Down: menu  Enter: open    Palette:jungle
General settings
  Audio input devices (D):Speakers (Realtek Audio)
  Application name:aUdioNLZR
```

## Line reference

**1** — Title breadcrumb (row 0): `appName/settings` per [ADR-0060](adr/0060-universal-title-breadcrumb.md).

**2** — Toolbar: hint text (left) and palette name with beat-reactive coloring (right), same palette phase as Preset/Show.

**3** — Hub section title: "General settings".

**4** — Blank line.

**5** — Menu: **Audio input devices (D):**current device display name (prefix `>` when selected). Mirrors **D** / device modal flow. If no device name is available, the value shows an em dash (—). Long values truncate to the terminal width.

**6** — Menu: **Application name:**effective short name (same rules as the title bar: `TitleBarAppName` when set, otherwise derived from `Title`). Prefix `>` when selected. **Enter** opens rename; printable keys edit; **Enter** confirms; **Esc** cancels.

When **Application name** is being edited, an additional line **Edit:** appears below the menu with the current buffer (implementation may truncate).

**Note:** Full screen (**F**) is disabled in General settings (fullscreen is cleared when entering General settings). The toolbar and hub remain visible when the window is wide enough.
