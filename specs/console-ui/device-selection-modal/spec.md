# UI spec: Device selection modal

## Blueprint

### Context

Console UI surface documented with ASCII screen dumps and line references per [format](../format/spec.md) and [ADR-0046](../../../docs/adr/0046-screen-dump-ascii-screenshot.md).

On **macOS**, the list includes **Demo modes**, **Desktop / system audio (Core Audio tap)** on **14.2+** (shim + **System Audio Recording**; label hints when the shim is missing — [ADR-0087](../../../docs/adr/0087-macos-core-audio-tap-system-audio.md)), and **Core Audio** physical inputs (microphones plus any manually configured virtual sinks such as BlackHole). The Core Audio tap is the **only** "what you hear" entry; the former **virtual-routing** and **ScreenCaptureKit** rows were removed ([ADR-0088](../../../docs/adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)). Long lists **scroll** within the console window (hint line when applicable). It does **not** imply **WASAPI-style built-in loopback**; desktop audio requires the Core Audio tap (built shim + consent) and the macOS console must run from an **ad-hoc signed `.app` bundle** for TCC ([ADR-0084](../../../docs/adr/0084-macos-multi-target-and-platform-audio.md)). **Windows** builds continue to show loopback entries per WASAPI.

### Architecture

Full-screen modal for choosing an audio input device. Opened from **D** in Preset / Show modes, or **Enter** on **Audio input devices** in [General settings hub](../general-settings-hub/spec.md). See [ui-spec-settings-surfaces.md](../settings-surfaces/spec.md) for shared patterns. List selection: [ui-spec-menu-selection.md](../menu-selection/spec.md).

## Screenshot

```text
aUdioNLZR/auDioinput
  Use ↑/↓ to select, ENTER to confirm, ESC to cancel

 ► Speakers (Realtek Audio) (current)
   Microphone (USB)
```

*(Representative layout; regenerate from a screen dump when verifying.)*

## Line reference

**1** — Title breadcrumb (row 0): app-settings track includes `/audioinput` per [ui-spec-title-breadcrumb.md](../title-breadcrumb/spec.md).

**2** — Hint: `  Use ↑/↓ to select, ENTER to confirm, ESC to cancel`, optionally suffixed with ` — list scrolls when longer than the window` when the device count exceeds the viewport (two leading spaces on the base hint).

**3** — Blank line.

**4…N** — Device list viewport: at most **window height minus header rows** lines, vertically scrolled so the selected index stays visible (**`SettingsSurfacesListDrawing.ComputeListScrollOffset`**). Each visible line is one device row, or blank padding when the viewport extends past the end of the list. Selected line uses leading ` ► ` (space + U+25BA); other lines use three spaces. The active capture device is suffixed with ` (current)` when its name matches. Selected row uses UI palette background + highlighted foreground; the current (non-selected) device line may use highlighted foreground only. Long names truncate to terminal width minus margin.

The modal uses **ModalSystem.RunModal** (full redraw on key). Closing restores the main view breadcrumb.

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
