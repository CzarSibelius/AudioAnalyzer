# PBI-006: Layer type picker (L) from preset settings modal

**Transient work item** — close after merge. **The Spec** (`specs/console-ui/preset-editor-navigation/spec.md` and `specs/console-ui/preset-settings-modal/spec.md`) holds **state**; this file holds **delta**.

## Directive

While the **S** overlay is open in Preset editor, **L** (no modifiers) must open the same **layer type picker** as on the main canvas ([`ILayerPickerModal`](../src/AudioAnalyzer.Console/ILayerPickerModal.cs) / [`LayerPickerModal`](../src/AudioAnalyzer.Console/Console/LayerPickerModal.cs)), subject to:

1. **Focus:** Only when **L** is unambiguous (e.g. **LayerList** per spec); **not** during rename or string edit. When the **Preset** row is selected in the left column, **L** is a **no-op** (spec scenario: no picker).
2. **Target slot:** The type applied on **Enter** must match the **selected layer row** in **S**, not a stale [`GetActiveLayerZIndex`](../src/AudioAnalyzer.Application/Abstractions/IVisualizer.cs) if that index can diverge—**sync** active sorted index before `Show`, **or** extend the picker API with an explicit sorted slot (spec Architecture).
3. **Nested modal / render guard:** Closing the picker while **S** remains open must **not** set the shell’s global “modal open” / render guard to false ([`ApplicationShell`](../src/AudioAnalyzer.Console/ApplicationShell.cs) `modalOpen`; today [`LayerPickerModal`](../src/AudioAnalyzer.Console/Console/LayerPickerModal.cs) calls `setModalOpen(false)` on close). Use a wrapper callback, refcount, or equivalent so only the **outer** modal owns clearing that flag.

**In scope:** [`SettingsModalKeyHandler`](../src/AudioAnalyzer.Console/SettingsModal/SettingsModalKeyHandler.cs), [`SettingsModalKeyContext`](../src/AudioAnalyzer.Console/SettingsModal/SettingsModalKeyContext.cs), [`SettingsModal`](../src/AudioAnalyzer.Console/Console/SettingsModal.cs) DI wiring if needed, [`LayerPickerModal`](../src/AudioAnalyzer.Console/Console/LayerPickerModal.cs) / [`ILayerPickerModal`](../src/AudioAnalyzer.Console/ILayerPickerModal.cs) only as required for slot parameter and/or `setModalOpen` semantics, hint text in [`SettingsModalRenderer`](../src/AudioAnalyzer.Console/SettingsModal/SettingsModalRenderer.cs) if the modal hint line should mention **L**, help/bindings for **H**.

**Out of scope:** **L** from other modals (device, help); changing picker behavior when opened from the main loop (PBI-004 / existing code) except shared fixes (e.g. `setModalOpen` contract if generalized).

## Context pointer

- Primary spec: [`specs/console-ui/preset-editor-navigation/spec.md`](../specs/console-ui/preset-editor-navigation/spec.md) (Architecture “Layer type picker overlay”, Constraints “L from S modal”, Scenarios for **L** while **S** open, **L** with Preset row, **Layer picker close does not drop S modal guard**)
- Modal spec: [`specs/console-ui/preset-settings-modal/spec.md`](../specs/console-ui/preset-settings-modal/spec.md) (**LayerList** hint, Architecture **L** sentence)
- Related: [ADR-0069](../docs/adr/0069-unified-menu-selection-affordance.md), [`ModalSystem.RunOverlayModal`](../src/AudioAnalyzer.Console/Console/ModalSystem.cs)

## Verification pointer

- Contract: all Gherkin scenarios in the primary spec that mention **L** + **S**, plus regression guardrail on modal guard after inner close.
- Build / test / format: root [`AGENTS.md`](../AGENTS.md).

## Refinement rule

If nested overlay behavior forces a product decision not captured in the spec, **update** the navigation and/or preset-settings-modal spec **in the same commit** (same-commit rule); escalate ambiguous product calls to human review.
