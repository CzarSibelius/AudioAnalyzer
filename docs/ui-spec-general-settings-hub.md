# UI spec: General Settings hub

**Application mode:** `ApplicationMode.Settings` ([`ApplicationMode`](../src/AudioAnalyzer.Domain/ApplicationMode.cs)). Listed in [ui-spec-application-modes.md](ui-spec-application-modes.md).

This layout applies when **ApplicationMode** is **General settings** (`ApplicationMode.Settings`): **Tab** from Preset editor or Show play (when eligible). The **header is one row** (title breadcrumb only); Device/Now/BPM/Beat/Volume rows are not shown ([ADR-0062](adr/0062-application-mode-classes.md)).

Shared vocabulary and feature matrix: [ui-spec-settings-surfaces.md](ui-spec-settings-surfaces.md). Selectable row affordance: [ui-spec-menu-selection.md](ui-spec-menu-selection.md). Hub menu rows render via **HorizontalRowComponent** (single full-width preformatted cell) for alignment with other single-line row components ([ADR-0057](adr/0057-horizontal-row-unified-single-line-rows.md)).

## Screenshot

```text
aUdioNLZR/sEttings
General settings — Tab: mode  Up/Down: menu  Enter: open  BPM: cycle source    Palette:jungle
General settings
 ► Audio input devices (D):Speakers (Realtek Audio)
   BPM source (Enter):Audio (beat detect)
   Application name:aUdioNLZR
   Default asset folder:(App base)
   UI theme (T):(Custom)
   Show render FPS (Enter):Off
```

(Plain-text mockup; on screen the hub title and menu **values** use per-grapheme palette colors with the same beat/tick phase as the toolbar palette swatch.)

## Line reference

- **1** — Title breadcrumb (row 0): `appName/settings` per [ADR-0060](adr/0060-universal-title-breadcrumb.md).
- **2** — Toolbar: hint text (left) and palette name with beat-reactive coloring (right), same palette phase as Preset/Show. Hint includes **BPM: cycle source** for the BPM source row.
- **3** — Hub section title: "General settings", with beat/tick-driven per-grapheme colors (same phase as line 2 palette swatch; see below).
- **4** — Blank line.
- **5** — Menu: **Audio input devices (D):** current device display name. Selected row: leading **` ► `**, full-line **UI theme** selection background + highlighted foreground ([ui-spec-menu-selection.md](ui-spec-menu-selection.md)). Unselected: three spaces before the label; value uses the same palette phase as the toolbar palette cell: colors from the current `UiThemePaletteId` layer palette when set; when **(Custom)** (no theme id), colors cycle through the effective semantic `UiPalette` slots. Mirrors **D** / device modal flow. If no device name is available, the value shows an em dash (—). Long values scroll or truncate per the horizontal row viewport.
- **6** — Menu: **BPM source (Enter):** one of **Audio (beat detect)**, **Demo (time + demo device BPM)**, or **Ableton Link** ([ADR-0066](adr/0066-bpm-source-and-ableton-link.md)). Same selection affordance as line **5** when this row is selected. **Enter** cycles the source; beat timing updates immediately; settings persist on save.
- **7** — Menu: **Application name:** effective short name (same rules as the title bar: `TitleBarAppName` when set, otherwise derived from `Title`). Same selection affordance as line **5**. **Enter** opens rename; printable keys edit; **Enter** confirms; **Esc** cancels.
- **8** — Menu: **Default asset folder:** when `UiSettings.DefaultAssetFolderPath` is unset, the value shows **`(App base)`** (layers with empty image/model folder use `AppContext.BaseDirectory` as the global base); when set, the configured path (trimmed). Same selection affordance as line **5**. **Enter** opens path edit; printable keys and **Backspace** edit; **Enter** confirms (empty buffer clears the setting); **Esc** cancels.
- **9** — Menu: **UI theme (T):** palette display name, **`(Custom)`** when `UiThemePaletteId` is unset (inline `UiSettings.Palette` / `TitleBarPalette` in appsettings). Unselected value coloring matches line **5**; selected row uses solid selection colors (no swatch animation on that row). Same **` ► `** affordance when selected. **Enter** or **T** opens the theme list (**(Custom)** clears the theme id; otherwise sets `UiThemePaletteId` to the chosen layer palette id). **Esc** cancels the modal without changing the theme. In the theme list modal, palette names use the same beat/tick-driven per-letter colors as the S-modal palette picker; the list idle-redraws when the palette name animation frame advances (`PaletteSwatchFormatter.PaletteAnimationFrameAdvanced`).
- **10** — Menu: **Show render FPS (Enter):** **`On`** or **`Off`** for `UiSettings.ShowRenderFps` ([ADR-0067](adr/0067-60fps-target-and-render-fps-overlay.md)). When on, the main toolbar shows a smoothed **FPS** value for full visualization redraws. Same selection affordance as line **5**. **Enter** toggles and persists settings.

When **Application name** or **Default asset folder** is being edited, an additional line **Edit:** appears below the menu with the current buffer (implementation may truncate).

**Note:** Full screen (**F**) is disabled in General settings (fullscreen is cleared when entering General settings). The toolbar and hub remain visible when the window is wide enough.
