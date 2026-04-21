# PBI-007: Delete preset from S modal (Preset row + Delete)

**Transient work item** — close after merge. **The Spec** (`specs/console-ui/preset-settings-modal/spec.md`) holds **state**; this file holds **delta**.

## Directive

Implement **preset-level delete**: when the preset / layer settings modal (**S**) is open with **LayerList** focus and the left-column **Preset** row is selected, plain **Delete** must, when **two or more** presets exist: first compute `nextActiveId` = [`PresetNavigationOrder.GetNextPresetIdByDisplayName`](../../src/AudioAnalyzer.Domain/PresetNavigationOrder.cs)(**current** `Presets` list, current `ActivePresetId`) — **before** calling `Delete` so the successor matches **V** and is not recomputed from a list that no longer contains the deleted id — then call [`IPresetRepository.Delete`](../../src/AudioAnalyzer.Application/Abstractions/IPresetRepository.cs)(former active id), refresh `Presets` from `GetAll()`, set `ActivePresetId` to `nextActiveId`, load `TextLayers` from the new active preset, persist, and notify the host/visualizer (same invariants as switching presets with **V**). When **only one** preset exists, **Delete** on the **Preset** row must be a **no-op** (today’s early return in [`SettingsModalKeyHandler`](../../src/AudioAnalyzer.Console/SettingsModal/SettingsModalKeyHandler.cs) for `LeftPanelPresetSelected` + Delete becomes product behavior: keep no-op, optionally document if any feedback is added).

When a **layer** row is selected, **Delete** must continue to remove only the selected layer per [ADR-0070](../../docs/adr/0070-settings-modal-add-remove-layers.md).

**In scope:** [`SettingsModalKeyHandler`](../../src/AudioAnalyzer.Console/SettingsModal/SettingsModalKeyHandler.cs) (or extracted helper), any [`SettingsModalKeyContext`](../../src/AudioAnalyzer.Console/SettingsModal/SettingsModalKeyHandler.cs) / shell callback needed to reload `TextLayers` and sync [`ITextLayerStateStore`](../../src/AudioAnalyzer.Visualizers/TextLayers/ITextLayerStateStore.cs) like a preset switch; [`SettingsModalRenderer`](../../src/AudioAnalyzer.Console/SettingsModal/SettingsModalRenderer.cs) hint line; help / key-binding lists if the project surfaces **S** modal keys there.

**Out of scope:** Deleting presets from General settings; a dedicated global hotkey on the main canvas; undo; confirmation dialog (spec does not require one — add only if product revises the spec).

## Context pointer

- Primary spec: [`specs/console-ui/preset-settings-modal/spec.md`](../specs/console-ui/preset-settings-modal/spec.md) (Architecture preset delete, **Constraints** “Delete disambiguation” / “At least one preset” / “After a successful preset delete”, **Contract**, **Scenarios** for delete)
- Related: [`specs/console-ui/preset-editor-navigation/spec.md`](../specs/console-ui/preset-editor-navigation/spec.md) (cross-link: preset delete not on canvas)
- ADRs: [ADR-0022](../../docs/adr/0022-presets-in-own-files.md) (persistence + new decision item 8), [ADR-0019](../../docs/adr/0019-preset-textlayers-configuration.md) (at least one preset), [ADR-0070](../../docs/adr/0070-settings-modal-add-remove-layers.md) (layer delete)

## Verification pointer

- Contract: **Definition of Done** and **Scenarios** in the preset-settings-modal spec for delete / single-preset no-op / layer delete unchanged.
- Build / test / format: root [`AGENTS.md`](../AGENTS.md). Add or extend tests if there is an existing pattern for settings modal or shell preset switching.

## Refinement rule

If successor ordering or modal state after delete needs a different rule, **update** [`preset-settings-modal/spec.md`](../specs/console-ui/preset-settings-modal/spec.md) **in the same commit** (same-commit rule).
