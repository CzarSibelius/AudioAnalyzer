# UI spec: General Settings hub

## Blueprint

### Context

Console UI surface documented with ASCII screen dumps and line references per [format](../format/spec.md) and [ADR-0046](../../../docs/adr/0046-screen-dump-ascii-screenshot.md).

### Architecture

**Application mode:** `ApplicationMode.Settings` ([`ApplicationMode`](../../../src/AudioAnalyzer.Domain/ApplicationMode.cs)). Listed in [ui-spec-application-modes.md](../application-modes/spec.md).

This layout applies when **ApplicationMode** is **General settings** (`ApplicationMode.Settings`): **Tab** from Preset editor or Show play (when eligible). The **header is one row** (title breadcrumb only); Device/Now/BPM/Beat/Volume rows are not shown ([ADR-0062](../../../docs/adr/0062-application-mode-classes.md)).

Shared vocabulary and feature matrix: [ui-spec-settings-surfaces.md](../settings-surfaces/spec.md). Selectable row affordance: [ui-spec-menu-selection.md](../menu-selection/spec.md). Hub menu rows render via **HorizontalRowComponent** (single full-width preformatted cell) for alignment with other single-line row components ([ADR-0057](../../../docs/adr/0057-horizontal-row-unified-single-line-rows.md)).

## Screenshot

```text
aUdioNLZR/sEttings
General settings — Tab: mode  Up/Down: menu  Enter: open  BPM: cycle  +/−: max audio history (row)  Q/Esc: quit    Palette:jungle
General settings

 ► Audio input devices (D):Speakers (Realtek Audio)
   BPM source (Enter):Audio (beat detect)
   Application name:aUdioNLZR
   Max audio history (+/− Enter):60 s
   Default asset folder:(App base)
   UI theme (T):(Custom)
   Show render FPS (Enter):Off
   Show layer render time (Enter):Off

Feature status
   Audio capture:available (System Audio (Loopback))
   Ableton Link:unavailable (native link_shim not loaded)
   ASCII video (webcam):available
   Now playing:available
   Screen dump:available
```

(Plain-text mockup; on screen the hub title and menu **values** use per-grapheme palette colors with the same beat/tick phase as the toolbar palette swatch. The example shows a **Windows** host; the **Feature status** lines vary by platform — see line reference **13–14**.)

## Line reference

- **1** — Title breadcrumb (row 0): `appName/settings` per [ADR-0060](../../../docs/adr/0060-universal-title-breadcrumb.md).
- **2** — Toolbar: hint text (left) and palette name with beat-reactive coloring (right), same palette phase as Preset/Show. Hint includes **BPM: cycle** for the BPM source row, **+/−: max audio history** when adjusting the **Max audio history** row, and **Q/Esc: quit** (opens the [quit confirmation](../quit-confirmation-modal/spec.md); hub-menu Escape falls through to it per [ADR-0093](../../../docs/adr/0093-confirm-before-quit-and-deliberate-quit-keys.md)).
- **3** — Hub section title: "General settings", with beat/tick-driven per-grapheme colors (same phase as line 2 palette swatch; see below).
- **4** — Blank line.
- **5** — Menu: **Audio input devices (D):** current device display name. Selected row: leading **` ► `**, full-line **UI theme** selection background + highlighted foreground ([ui-spec-menu-selection.md](../menu-selection/spec.md)). Unselected: three spaces before the label; value uses the same palette phase as the toolbar palette cell: colors from the active theme’s **`FallbackPaletteId`** layer palette when set; when **(Custom)** (no `UiThemeId`) or the theme has no fallback, colors cycle through the effective semantic `UiPalette` slots. Mirrors **D** / device modal flow. If no device name is available, the value shows an em dash (—). Long values scroll or truncate per the horizontal row viewport.
- **6** — Menu: **BPM source (Enter):** one of **Audio (beat detect)**, **Demo (time + demo device BPM)**, or **Ableton Link** ([ADR-0066](../../../docs/adr/0066-bpm-source-and-ableton-link.md)). Same selection affordance as line **5** when this row is selected. **Enter** cycles the source; beat timing updates immediately; settings persist on save.
- **7** — Menu: **Application name:** effective short name (same rules as the title bar: `TitleBarAppName` when set, otherwise derived from `Title`). Same selection affordance as line **5**. **Enter** opens rename; printable keys edit; **Enter** confirms; **Esc** cancels.
- **8** — Menu: **Max audio history (+/− Enter):** seconds of mono waveform retained for the waveform-strip overview (`AppSettings.MaxAudioHistorySeconds`, clamped **5–180**; default **60**). Same selection affordance as line **5**. **+** / **−** (main or numpad) step by **5** seconds, clamp, resize the analysis ring via `IWaveformHistoryConfigurator`, and persist. **Enter** opens numeric edit (invariant culture); **Enter** commits parsed seconds (clamped); **Esc** cancels. See [ADR-0077](../../../docs/adr/0077-waveform-overview-snapshot.md).
- **9** — Menu: **Default asset folder:** when `UiSettings.DefaultAssetFolderPath` is unset, the value shows **`(App base)`** (layers with empty image/model folder use `AppContext.BaseDirectory` as the global base); when set, the configured path (trimmed). Same selection affordance as line **5**. **Enter** opens path edit; printable keys and **Backspace** edit; **Enter** confirms (empty buffer clears the setting); **Esc** cancels.
- **10** — Menu: **UI theme (T):** theme display name from `themes/*.json`, **`(Custom)`** when `UiThemeId` is unset (inline `UiSettings.Palette` / `TitleBarPalette` in appsettings). Unselected value coloring matches line **5**; selected row uses solid selection colors (no swatch animation on that row). Same **` ► `** affordance when selected. **Enter** or **T** opens the theme list (**(Custom)** clears `UiThemeId`; otherwise sets it to the chosen theme file id). **N** in the modal starts **new theme from palette** (palette pick → 11-slot index editor → save). **Esc** cancels the modal or steps back in the authoring sub-flow. In the theme list, rows with **`FallbackPaletteId`** use the same beat/tick-driven per-letter colors as the S-modal palette picker where applicable; the list idle-redraws when the frame advances (`PaletteSwatchFormatter.PaletteAnimationFrameAdvanced`).
- **11** — Menu: **Show render FPS (Enter):** **`On`** or **`Off`** for `UiSettings.ShowRenderFps` ([ADR-0067](../../../docs/adr/0067-60fps-target-and-render-fps-overlay.md)). When on, the main toolbar shows a smoothed **FPS** value for full main-area redraws (main-loop cadence—typically **≥~60** FPS on capable hosts). Same selection affordance as line **5**. **Enter** toggles and persists settings.
- **12** — Menu: **Show layer render time (Enter):** **`On`** or **`Off`** for `UiSettings.ShowLayerRenderTime` ([ADR-0073](../../../docs/adr/0073-layer-render-time-overlay.md)). When on, the **S** modal left column appends a compact per-layer **`Draw`** duration (from the last completed main render) next to each layer line. Same selection affordance as line **5**. **Enter** toggles and persists settings.
- **13** — Blank line, then a **read-only** section title **"Feature status"** (beat/tick-driven per-grapheme colors, same phase as the hub title). This section is **not selectable**: it is excluded from Up/Down navigation and `GeneralSettingsHubMenuRows.Count`, so the selection cursor never lands on it ([ADR-0095](../../../docs/adr/0095-feature-capability-report.md)).
- **14** — One **read-only** status line per applicable feature capability (from `IFeatureCapabilityReport`): **`name:available`** or **`name:unavailable`**, with an optional dimmed **(detail)** reason/hint; no **` ► `** affordance and no selection highlight. Capabilities reported as **not applicable** for the current host are hidden. Lines reflect a snapshot taken on entering Settings mode (not probed per frame, per [ADR-0030](../../../docs/adr/0030-performance-priority.md)). **Platform variance:** on **Windows** the rows are typically **Audio capture** (WASAPI), **Ableton Link** (`link_shim.dll`), **ASCII video (webcam)** (WinRT MediaCapture), **Now playing** (GSMTC), **Screen dump** (kernel32). On **macOS** the rows include **Audio capture** (Core Audio mic/input), **System audio tap** (`libaudio_tap_shim.dylib`), **ASCII video (webcam)** (`libvideo_capture_shim.dylib`), **Now playing** (`mediaremote-adapter`), **Screen dump** (unavailable), plus **permission** rows — **System Audio Recording**, **Microphone**, **Camera** — whose availability comes from **non-prompting** TCC preflight / authorization-status queries that never raise a consent prompt ([ADR-0095](../../../docs/adr/0095-feature-capability-report.md), [ADR-0091](../../../docs/adr/0091-macos-tap-explicit-consent-output-driven-aggregate-stable-signing.md)).

When a row is in inline text edit (application name, default asset folder, or max audio history), an additional line **Edit:** appears with the current buffer (implementation may truncate). The **Edit:** line is drawn **between the last menu row and the Feature status section** so the status block stays anchored at the bottom of the hub.

**Note:** Full screen (**F**) is disabled in General settings (fullscreen is cleared when entering General settings). The toolbar and hub remain visible when the window is wide enough.

### Constraints

- **8-column blocks** and **Label:value** formatting per [ADR-0050](../../../docs/adr/0050-ui-alignment-blocks-label-format.md).
- Regenerate screenshot + **Line reference** when layout or semantics change.

## Contract

### Definition of Done

- Screenshot block matches a fresh screen dump when rows or labels change.
- Every screen line in the dump has a matching **Line reference** entry.

### Regression guardrails

- Cross-links to other console-ui specs and ADRs resolve after moves under specs/console-ui/.

### Scenarios

```gherkin
Scenario: Capture matches spec
  Given the documented mode is active in a Windows console
  When the operator triggers a screen dump (Ctrl+Shift+E per ADR-0046)
  Then pasted ASCII matches the spec screenshot block line-for-line for controlled fixtures
```
