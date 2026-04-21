# PBI-004: Preset editor navigation

**Transient work item** — close after merge. **The Spec** (`specs/console-ui/preset-editor-navigation/spec.md`) holds **state**; this file holds **delta**.

## Directive

Deliver three Preset editor UX improvements, all gated to **Preset editor** mode unless noted:

1. **Alphabetical preset cycle (V):** Change preset stepping so **V** follows **display name** order (case-insensitive, trimmed; empty name sorts by id), with stable id tie-break—not merely `Presets` list / file-id order.
2. **Previous preset (Shift+V):** In Preset editor, **Shift+V** activates the **previous** preset in the same **display-name** order as **V** (wrapping). **Plain V** must remain distinguishable (no accidental forward cycle when the Shift+V chord is intended); **ConsoleShiftLetterV** handles terminals that omit Shift in `ConsoleKeyInfo`.
3. **Layer type picker:** Add an overlay UI (keyboard list per [ADR-0069](../docs/adr/0069-unified-menu-selection-affordance.md)) invokable from Preset editor with default key **L** listing **every** `TextLayerType` so the operator can assign a type to the **active 1–9 slot** (digit keys still choose the slot); **Enter** applies the highlighted type to that slot, **Esc** cancels; must integrate with the existing modal stack (no nested-modal bugs).

**In scope:** `[specs/console-ui/preset-editor-navigation/spec.md](../specs/console-ui/preset-editor-navigation/spec.md)`, `[ApplicationShell](../src/AudioAnalyzer.Console/ApplicationShell.cs)`, `[MainLoopKeyHandler` / context](../src/AudioAnalyzer.Console/KeyHandling/), optional new modal/overlay types under `src/AudioAnalyzer.Console/`, `[TextLayersVisualizer](../src/AudioAnalyzer.Visualizers/TextLayers/TextLayersVisualizer.cs)` / `[TextLayersKeyHandler](../src/AudioAnalyzer.Visualizers/TextLayers/TextLayersKeyHandler.cs)` if layer index mutation is centralized, `[HelpModal](../src/AudioAnalyzer.Console/Console/HelpModal.cs)` and key-binding discovery strings, targeted tests.

**Out of scope:** Reordering presets in the **S** modal left column (unless needed for consistency—default **no**); **Show play** preset sequencing; symmetric “next layer” key (not requested—digit keys remain).

## Context pointer

- Primary spec: `[specs/console-ui/preset-editor-navigation/spec.md](../specs/console-ui/preset-editor-navigation/spec.md)`
- Related: [console-ui preset editor mode](../specs/console-ui/preset-editor-mode/spec.md), [text-layers hub](../specs/text-layers-visualizer/spec.md), [ADR-0019](../docs/adr/0019-preset-textlayers-configuration.md), [ADR-0069](../docs/adr/0069-unified-menu-selection-affordance.md)

## Verification pointer

- Satisfy **Contract** scenarios in the primary spec (preset order, Shift+V wrap, picker confirm/cancel).
- Build / test / format: root `[AGENTS.md](../AGENTS.md)`.

## Refinement rule

If ordering rules or key chords need product tweaks, **update the spec in the same commit** (same-commit rule). If **L** conflicts with another feature, document the chosen key in the spec and help in the same commit.

**Follow-on PBIs** (same spec hub, split for delivery): [PBI-005](./PBI-005-preset-editor-insert-delete-canvas.md) (Insert/Delete on canvas when **S** closed), [PBI-006](./PBI-006-layer-picker-from-s-modal.md) (**L** from **S** modal + nested guard + slot sync), [PBI-007](./PBI-007-delete-preset.md) (delete entire preset: **S** modal, **Preset** row + **Delete**; spec: [preset-settings-modal](../specs/console-ui/preset-settings-modal/spec.md)).