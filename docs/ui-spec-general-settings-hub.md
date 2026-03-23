# UI spec: General Settings hub

**Application mode:** `ApplicationMode.Settings` ([`ApplicationMode`](../src/AudioAnalyzer.Domain/ApplicationMode.cs)). Listed in [ui-spec-application-modes.md](ui-spec-application-modes.md).

This layout applies when **ApplicationMode** is **General settings** (`ApplicationMode.Settings`): **Tab** from Preset editor or Show play (when eligible). The **header is one row** (title breadcrumb only); Device/Now/BPM/Volume rows are not shown ([ADR-0062](adr/0062-application-mode-classes.md)).

Shared vocabulary and feature matrix: [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md). Hub menu rows render via **HorizontalRowComponent** (single full-width preformatted cell) for alignment with other single-line row components ([ADR-0057](adr/0057-horizontal-row-unified-single-line-rows.md)).

## Screenshot

```text
aUdioNLZR/sEttings
General settings — Tab: mode  Up/Down: menu  Enter: open    Palette:jungle
General settings
  Audio input devices (D):Speakers (Realtek Audio)
  Application name:aUdioNLZR
  UI theme (T):(Custom)
```

(Plain-text mockup; on screen the hub title and menu **values** use per-grapheme palette colors with the same beat/tick phase as the toolbar palette swatch.)

## Line reference

- **1** — Title breadcrumb (row 0): `appName/settings` per [ADR-0060](adr/0060-universal-title-breadcrumb.md).
- **2** — Toolbar: hint text (left) and palette name with beat-reactive coloring (right), same palette phase as Preset/Show.
- **3** — Hub section title: "General settings", with beat/tick-driven per-grapheme colors (same phase as line 2 palette swatch; see below).
- **4** — Blank line.
- **5** — Menu: **Audio input devices (D):**current device display name (prefix `>` when selected). Value uses the same palette phase as the toolbar palette cell: colors from the current `UiThemePaletteId` layer palette when set; when **(Custom)** (no theme id), colors cycle through the effective semantic `UiPalette` slots (Normal, Highlighted, Dimmed, Label, optional Background). Mirrors **D** / device modal flow. If no device name is available, the value shows an em dash (—). Long values scroll or truncate per the horizontal row viewport.
- **6** — Menu: **Application name:**effective short name (same rules as the title bar: `TitleBarAppName` when set, otherwise derived from `Title`). Value coloring matches line **5**. Prefix `>` when selected. **Enter** opens rename; printable keys edit; **Enter** confirms; **Esc** cancels.
- **7** — Menu: **UI theme (T):**palette display name, **`(Custom)`** when `UiThemePaletteId` is unset (inline `UiSettings.Palette` / `TitleBarPalette` in appsettings). Value coloring matches line **5**. Prefix `>` when selected. **Enter** or **T** opens the theme list (**(Custom)** clears the theme id; otherwise sets `UiThemePaletteId` to the chosen layer palette id). **Esc** cancels the modal without changing the theme. In the theme list modal, palette names use the same beat/tick-driven per-letter colors as the S-modal palette picker; the list idle-redraws when the palette name animation frame advances (`PaletteSwatchFormatter.PaletteAnimationFrameAdvanced`).

When **Application name** is being edited, an additional line **Edit:** appears below the menu with the current buffer (implementation may truncate).

**Note:** Full screen (**F**) is disabled in General settings (fullscreen is cleared when entering General settings). The toolbar and hub remain visible when the window is wide enough.
