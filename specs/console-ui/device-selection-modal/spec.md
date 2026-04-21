# UI spec: Device selection modal

## Blueprint

### Context

Console UI surface documented with ASCII screen dumps and line references per [format](../format/spec.md) and [ADR-0046](../../../docs/adr/0046-screen-dump-ascii-screenshot.md).

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

**2** — Hint: `  Use ↑/↓ to select, ENTER to confirm, ESC to cancel` (two leading spaces).

**3** — Blank line.

**4+** — Device list: one line per device. Selected line uses leading ` ► ` (space + U+25BA); other lines use three spaces. The active capture device is suffixed with ` (current)` when its name matches. Selected row uses UI palette background + highlighted foreground; the current (non-selected) device line may use highlighted foreground only. Long names truncate to terminal width minus margin.

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
