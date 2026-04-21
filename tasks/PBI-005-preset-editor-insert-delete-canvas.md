# PBI-005: Preset editor Insert/Delete on main canvas

**Transient work item** — close after merge. **The Spec** (`specs/console-ui/preset-editor-navigation/spec.md`) holds **state**; this file holds **delta**.

## Directive

When **ApplicationMode.PresetEditor** is active and the **S** preset settings overlay is **not** open, plain **Insert** and **Delete** (no Ctrl/Shift/Alt) must add or remove a text layer with the **same semantics** as the existing **S** modal layer-list paths in [`SettingsModalKeyHandler`](../src/AudioAnalyzer.Console/SettingsModal/SettingsModalKeyHandler.cs) (max count, default layer factory, [`ITextLayerStateStore`](../src/AudioAnalyzer.Visualizers/TextLayers/ITextLayerStateStore.cs) slot removal on delete, persistence, structure notifications). **Delete** targets the **active sorted slot** (same as toolbar **1–9** / [`IVisualizer.GetActiveLayerZIndex`](../src/AudioAnalyzer.Application/Abstractions/IVisualizer.cs)).

**In scope:** [`MainLoopKeyHandlerConfig`](../src/AudioAnalyzer.Console/KeyHandling/MainLoopKeyHandler.cs) / [`MainLoopKeyContext`](../src/AudioAnalyzer.Console/KeyHandling/MainLoopKeyContext.cs) if new dependencies are needed, shared mutation helper or service if extracted from settings handler, [`HelpModal`](../src/AudioAnalyzer.Console/Console/HelpModal.cs) / binding lists for **H**, tests if the repo has a pattern for key-handler or shell behavior.

**Out of scope:** Changing **S** modal Ins/Del behavior; **Show play** / **General settings**; remapping keys.

## Context pointer

- Primary spec: [`specs/console-ui/preset-editor-navigation/spec.md`](../specs/console-ui/preset-editor-navigation/spec.md) (Architecture “Insert / Delete on canvas”, Constraints, Scenarios **Insert adds layer from main canvas**, **Delete removes active layer from main canvas**)
- Related ADR: [ADR-0070](../docs/adr/0070-settings-modal-add-remove-layers.md)

## Verification pointer

- Contract: **Definition of Done** bullets for Insert/Delete on canvas; **Regression guardrails** (other modes unchanged).
- Build / test / format: root [`AGENTS.md`](../AGENTS.md).

## Refinement rule

If canvas and modal semantics cannot stay identical without a spec tweak, **update** [`preset-editor-navigation/spec.md`](../specs/console-ui/preset-editor-navigation/spec.md) **in the same commit** (same-commit rule).
