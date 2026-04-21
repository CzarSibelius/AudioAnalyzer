# UI specs: Application modes

## Blueprint

### Context

Console UI surface documented with ASCII screen dumps and line references per [format](../format/spec.md) and [ADR-0046](../../../docs/adr/0046-screen-dump-ascii-screenshot.md).

### Architecture

Top-level **`ApplicationMode`** values ([`ApplicationMode`](../../../src/AudioAnalyzer.Domain/ApplicationMode.cs)) each have a dedicated UI spec with an **ASCII screenshot** (from the [screen dump](../../../docs/adr/0046-screen-dump-ascii-screenshot.md)) and a **line-by-line reference**. Shared format: [ui-spec-format.md](../format/spec.md).

| Mode | Enum | Spec | Toolbar (lines 1–4) | Main area |
|------|------|------|----------------------|-----------|
| **Preset editor** | `PresetEditor` | [ui-spec-preset-editor-mode.md](../preset-editor-mode/spec.md) | [ui-spec-toolbar.md](../toolbar/spec.md) — rows 1–3 from `HeaderContainer`, row 4 from main content | Visualizer from line 5 |
| **Show play** | `ShowPlay` | [ui-spec-show-play-mode.md](../show-play-mode/spec.md) | Same [Toolbar](../toolbar/spec.md) layout as Preset | Visualizer from line 5; presets auto-cycle per Show ([ADR-0031](../../../docs/adr/0031-show-preset-collection.md)) |
| **General settings** | `Settings` | [ui-spec-general-settings-hub.md](../general-settings-hub/spec.md) | 1 (title breadcrumb only) | Hub menu (**HorizontalRowComponent**); no visualizer ([ADR-0061](../../../docs/adr/0061-general-settings-mode.md), [ADR-0062](../../../docs/adr/0062-application-mode-classes.md)) |

**Visualizer fullscreen (F)** — edge-to-edge layout in Preset editor and Show play, per-mode `AllowsVisualizerFullscreen`, and interaction with overlay modals: [ui-spec-fullscreen-visualizer.md](../fullscreen-visualizer/spec.md).

**Tab** cycles: Preset editor → Show play (if at least one show has entries) → General settings → Preset editor ([README](../../../README.md) — **Usage** / keyboard controls).

## Settings surfaces and modals (all modes)

These are not separate `ApplicationMode` values but are documented with the same screenshot + line reference pattern:

- Index: [ui-spec-settings-surfaces.md](../settings-surfaces/spec.md)
- General settings hub (also listed above)
- [ui-spec-device-selection-modal.md](../device-selection-modal/spec.md)
- [ui-spec-preset-settings-modal.md](../preset-settings-modal/spec.md)
- Universal title breadcrumb (row 0): [ui-spec-title-breadcrumb.md](../title-breadcrumb/spec.md)

## Regenerating screenshots

1. **Interactive**: Run the app in a real Windows console, switch to the desired mode, press **Ctrl+Shift+E** (or use the **Print Screen** flow per [ADR-0046](../../../docs/adr/0046-screen-dump-ascii-screenshot.md)). Paste the `.txt` output into the spec’s fenced block and refresh the line reference.
2. **Automation**: `--dump-after N` / `--dump-path` work when the process has a **valid console** (e.g. local `cmd` or PowerShell). In environments without a console (some CI or automated hosts), the app may fail on `Console` APIs; use interactive capture instead.
3. Persisted mode is stored in `VisualizerSettings.ApplicationMode` in `appsettings.json` next to the executable ([ADR-0029](../../../docs/adr/0029-no-settings-migration.md)); set it before running if you need a specific mode on startup.

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
