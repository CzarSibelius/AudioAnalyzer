# Console UI (hub)

## Blueprint

### Context

The operator-facing surface is a **Windows console** UI with strict row alignment, shared toolbar patterns, and **ASCII screen dumps** as the visual contract ([ADR-0046](../../docs/adr/0046-screen-dump-ascii-screenshot.md)). This hub lists every **The Spec** file for console layout and modals.

### Architecture

- **Host / layout code**: `src/AudioAnalyzer.Console/`, `src/AudioAnalyzer.Application/` (toolbar builders, row layout).
- **Per-surface specs** (each `spec.md` uses Blueprint + Contract; screenshots live in **Architecture**):

| Surface | Spec |
|---------|------|
| Screenshot + line reference format | [format/spec.md](./format/spec.md) |
| Application modes (index) | [application-modes/spec.md](./application-modes/spec.md) |
| Preset editor mode | [preset-editor-mode/spec.md](./preset-editor-mode/spec.md) |
| Preset editor navigation (V / Shift+V preset order, layer picker) | [preset-editor-navigation/spec.md](./preset-editor-navigation/spec.md) |
| Show play mode | [show-play-mode/spec.md](./show-play-mode/spec.md) |
| General settings hub | [general-settings-hub/spec.md](./general-settings-hub/spec.md) |
| Fullscreen visualizer | [fullscreen-visualizer/spec.md](./fullscreen-visualizer/spec.md) |
| Settings surfaces index | [settings-surfaces/spec.md](./settings-surfaces/spec.md) |
| Device selection modal | [device-selection-modal/spec.md](./device-selection-modal/spec.md) |
| Preset settings modal | [preset-settings-modal/spec.md](./preset-settings-modal/spec.md) |
| Title breadcrumb | [title-breadcrumb/spec.md](./title-breadcrumb/spec.md) |
| Toolbar | [toolbar/spec.md](./toolbar/spec.md) |
| Menu selection | [menu-selection/spec.md](./menu-selection/spec.md) |
| Layer render bounds | [layer-render-bounds/spec.md](./layer-render-bounds/spec.md) |

- **Text layers / visualizer** (separate domain): [text-layers-visualizer hub](../text-layers-visualizer/spec.md).

### Constraints

- **Label:value** and **8-column blocks** per [ADR-0050](../../docs/adr/0050-ui-alignment-blocks-label-format.md).
- Regenerate **Screenshot** + **Line reference** together when rows change.

## Contract

### Definition of Done

- `dotnet build .\AudioAnalyzer.sln` with **0 warnings** after UI-affecting changes.
- `dotnet format .\AudioAnalyzer.sln --verify-no-changes` when layout-related strings or constants shift.
- Affected `specs/console-ui/**/spec.md` updated in the **same commit** as behavior.

### Regression guardrails

- **Tab** / mode transitions stay consistent with [application-modes/spec.md](./application-modes/spec.md) and linked mode specs.
- Screen dump capture workflow remains valid per [ADR-0046](../../docs/adr/0046-screen-dump-ascii-screenshot.md).

### Scenarios

```gherkin
Scenario: Screen dump matches line reference
  Given a documented mode is active in a real Windows console
  When the operator saves a screen dump to the spec's fenced block
  Then every line number in the Line reference section describes the same row in the block
```
