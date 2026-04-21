# Preset editor: preset cycle order, layer navigation, layer picker

## Blueprint

### Context

In **Preset editor** mode, operators switch **presets** and the **active text layer** (palette / contextual toolbar row, title breadcrumb slot) from the keyboard. Today **V** advances the active preset using the order of `VisualizerSettings.Presets` (which mirrors repository enumeration, currently **id-ordered** from [`FilePresetRepository.GetAll`](../../../src/AudioAnalyzer.Infrastructure/FilePresetRepository.cs)). On the live canvas, **1–9** select the active layer ([`TextLayersKeyHandlerConfig`](../../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayersKeyHandler.cs)); the **S** modal uses arrows and **+/-** in other focus paths. Operators want **alphabetical** preset traversal (**V** forward, **Shift+V** backward), and a **Layer type picker** overlay (**L**) to set the **layer type** of the active **1–9** slot from the full `TextLayerType` catalog (not only types already present in the preset stack).

### Architecture

- **Preset cycle (V / Shift+V):** [`ApplicationShell`](../../../src/AudioAnalyzer.Console/ApplicationShell.cs) applies the active preset from the repository after resolving the next or previous id via [`PresetNavigationOrder`](../../../src/AudioAnalyzer.Domain/PresetNavigationOrder.cs) (`GetNextPresetIdByDisplayName` / `GetPreviousPresetIdByDisplayName`). [`MainLoopKeyHandlerConfig`](../../../src/AudioAnalyzer.Console/KeyHandling/MainLoopKeyHandler.cs) binds **V** to `OnPresetCycle` and **Shift+V** to `OnPresetCyclePrevious` (after the visualizer returns), using [`ConsoleShiftLetterV`](../../../src/AudioAnalyzer.Console/KeyHandling/ConsoleShiftLetterV.cs) so terminals that omit Shift in `ConsoleKeyInfo` still distinguish the chord from plain **V**.
- **Layer index:** [`TextLayersVisualizer`](../../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayersVisualizer.cs) `_paletteCycleLayerIndex`; digit keys and **L** layer picker only (not **Shift+V**).
- **Layer type picker overlay:** Host-owned overlay (pattern: [`ModalSystem`](../../../src/AudioAnalyzer.Console/Console/ModalSystem.cs), [`DeviceSelectionModal`](../../../src/AudioAnalyzer.Console/Console/DeviceSelectionModal.cs)). **From the main Preset editor canvas** (no **S** modal), [`MainLoopKeyHandlerConfig`](../../../src/AudioAnalyzer.Console/KeyHandling/MainLoopKeyHandler.cs) invokes [`ILayerPickerModal`](../../../src/AudioAnalyzer.Console/ILayerPickerModal.cs) / [`LayerPickerModal`](../../../src/AudioAnalyzer.Console/Console/LayerPickerModal.cs) on **L**. **From the preset settings overlay (S)** the same picker must be reachable on **L** while the overlay has focus (nested overlay loop is acceptable). Before opening the picker from **S**, the **target sorted slot** must match operator intent: use the modal’s highlighted **layer** row when **Preset** is not selected; align with the same **1–9** sorted slot semantics as on the canvas (today [`LayerPickerModal`](../../../src/AudioAnalyzer.Console/Console/LayerPickerModal.cs) uses [`IVisualizer.GetActiveLayerZIndex`](../../../src/AudioAnalyzer.Application/Abstractions/IVisualizer.cs) — if the modal selection can diverge from that index, Dev must sync or pass an explicit slot so **L** changes the highlighted layer, not a stale toolbar slot). On picker close while **S** remains open, the application **modal-open / render-guard** state must stay consistent with **S** still blocking the main loop ([`ApplicationShell`](../../../src/AudioAnalyzer.Console/ApplicationShell.cs) `modalOpen`); inner [`LayerPickerModal`](../../../src/AudioAnalyzer.Console/Console/LayerPickerModal.cs) `setModalOpen(false)` must not imply “no modal” if **S** is still active (wrapper callback or refcount pattern). Rendering reuses list row styling from [menu selection affordance](../menu-selection/spec.md) / [ADR-0069](../../../docs/adr/0069-unified-menu-selection-affordance.md). The list enumerates every [`TextLayerType`](../../../src/AudioAnalyzer.Domain/VisualizerSettings/TextLayerType.cs) in stable name order ([`TextLayerPickerCatalog`](../../../src/AudioAnalyzer.Domain/TextLayerPickerCatalog.cs)). **Enter** applies the highlighted type to the **target sorted slot**; **Esc** leaves types unchanged. Digit keys still select **which** slot is active on the canvas; **L** does not change the slot index by itself.
- **Insert / Delete (layer stack) on canvas:** When **Preset editor** is active and the **S** modal is **not** open, **Insert** and **Delete** must add or remove a text layer with the **same semantics** as in the **S** modal layer list ([`SettingsModalKeyHandler`](../../../src/AudioAnalyzer.Console/SettingsModal/SettingsModalKeyHandler.cs) Insert/Delete paths, [ADR-0070](../../../docs/adr/0070-settings-modal-add-remove-layers.md)): respect max layer count, factory for new rows, [`ITextLayerStateStore`](../../../src/AudioAnalyzer.Visualizers/TextLayers/ITextLayerStateStore.cs) slot removal, persistence, and structure notifications. **Delete** removes the layer in the **active sorted slot** (same as active **1–9** / toolbar / [`GetActiveLayerZIndex`](../../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayersVisualizer.cs)). Prefer one shared mutation path or service so **S** and the main loop cannot drift.
- **Tests:** Console or application-level tests for ordering and key routing where the repo already tests shell or key handlers; visualizer tests for layer index if logic moves into a testable helper.

### Constraints

- **V** without modifiers: **Preset editor only**; unchanged requirement that other modes do not treat plain **V** as preset cycle ([ADR-0019](../../../docs/adr/0019-preset-textlayers-configuration.md)).
- **Shift+V**: **Preset editor only**; previous preset in the same **display-name** order as **V** (wrapped). Must not quit or open settings. If **layer bounds** edit session is active, keys follow existing precedence ([`PresetEditorApplicationMode.TryHandleVisualizerKeys`](../../../src/AudioAnalyzer.Console/PresetEditorApplicationMode.cs)): bounds handler first, then visualizer (main loop still receives the key when the visualizer does not consume it).
- **Alphabetical order:** Sort by **preset display name** (trimmed), **case-insensitive** (`StringComparer.OrdinalIgnoreCase`). When the trimmed name is empty, use **preset id** for that row’s sort key. **Stable tie-break:** compare **id** case-insensitively so order is deterministic.
- **Layer type picker:** **Esc** dismisses without changing any layer type. **Enter** applies the highlighted `TextLayerType` to the target slot (same mutation path as **←/→** type cycle on that slot, including clearing per-layer runtime state). Default open key: **L** (Preset editor). **L** works from the main canvas and while the **S** preset settings overlay is open (see Architecture). Other modals (device, help, etc.) unchanged unless explicitly extended.
- **Insert / Delete on canvas:** **Preset editor**, **S** closed, plain **Insert** / **Delete** (no Ctrl/Shift/Alt): add/remove layers per ADR-0070; must not fire when another application mode is active. Optional: ignore while a **text-edit** or rename field has focus inside **S** (N/A on canvas).
- **L from S modal:** Handled only when focus is **LayerList** (or another non–text-entry focus where **L** is unambiguous). When the **Preset** row is selected in the left column, **L** is a **no-op** (no picker; avoids an ambiguous target slot).

### Data / config

No new persisted fields are required for ordering; navigation is derived from existing preset names and ids.

---

## Contract

### Definition of Done

- With at least three presets whose **names** are not in file-id order, pressing **V** repeatedly visits presets in **ascending name order**, wrapping to the first after the last.
- **Shift+V** activates the **previous** preset in **descending** display-name step (inverse of **V**), wrapping from the first sorted preset to the last.
- **L** opens the **layer type picker** from Preset editor on the main canvas **and** while the **S** overlay is open; the operator can move selection with **Up/Down** and **+/-**, confirm with **Enter**, cancel with **Esc**; breadcrumb / toolbar reflect the new type for the target slot.
- With **S** closed, **Insert** / **Delete** add or remove layers on the active slot with the same limits and side effects as the **S** modal layer list.
- `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes` per [AGENTS.md](../../../AGENTS.md); help text (**H**) and static key-binding lists include **V**, **Shift+V**, **L**, **Insert**, and **Delete** where applicable.
- `specs/console-ui/preset-editor-navigation/spec.md` and hub links updated in the **same commit** as behavior.

### Regression guardrails

- **Show play**, **General settings**, and **S** modal behavior for keys that are not intentionally changed remain intact.
- Closing only the layer picker while **S** is still open does not clear the global “modal open” render guard prematurely.
- **Plain V** must not fire forward preset cycle when **Shift+V** chord is detected (reserved for **Shift+V** previous preset).

### Scenarios

```gherkin
Scenario: V cycles presets by display name
  Given Preset editor is active
  And presets exist with display names out of alphabetical id order
  When the user presses V repeatedly until the list wraps
  Then each step activates the next preset in ascending trimmed name order (case-insensitive)
  And ties on name are broken deterministically by id

Scenario: Shift V cycles presets backward by display name
  Given Preset editor is active
  And presets exist with display names out of alphabetical id order
  When the user presses Shift+V repeatedly until the list wraps
  Then each step activates the previous preset in descending trimmed name order (inverse of V)
  And ties on name are broken deterministically by id

Scenario: Shift V wraps from first preset in sorted order
  Given Preset editor is active
  And the active preset is the first in ascending display-name order
  When the user presses Shift+V
  Then the active preset becomes the last in that sorted order

Scenario: Layer type picker opens from L on canvas
  Given Preset editor is active and no modal is open
  When the user presses L
  Then the layer type picker overlay appears listing every TextLayerType

Scenario: Layer type picker opens from L while S modal is open
  Given Preset editor is active and the S preset settings overlay is open
  And a layer row is selected in the left column (not the Preset row)
  When the user presses L
  Then the layer type picker overlay appears
  And the slot to be edited matches that selected layer row

Scenario: L does not open picker when Preset row is selected in S modal
  Given Preset editor is active and the S preset settings overlay is open
  And the Preset row is selected in the left column
  When the user presses L
  Then the layer type picker does not open

Scenario: Layer picker close does not drop S modal guard
  Given Preset editor is active and the S modal is open
  And the user opened the layer type picker with L and then dismisses it with Esc
  When the picker has closed
  Then the S overlay is still active and the visualizer does not repaint as if no modal were open

Scenario: Layer type picker confirms selection
  Given Preset editor is active and the layer type picker is open
  And a layer slot is active (via 1-9)
  When the user moves highlight to a different layer type and presses Enter
  Then the picker closes
  And that slot's layer type matches the highlighted type

Scenario: Layer type picker cancel
  Given Preset editor is active and the layer type picker is open
  When the user presses Esc
  Then the picker closes
  And no layer type was changed from before the picker opened

Scenario: Insert adds layer from main canvas
  Given Preset editor is active and the S modal is not open
  And the layer count is below the maximum
  When the user presses Insert
  Then a new text layer is added with the same rules as Ins inside the S modal
  And settings are persisted

Scenario: Delete removes active layer from main canvas
  Given Preset editor is active and the S modal is not open
  And at least one text layer exists
  When the user presses Delete
  Then the active sorted slot layer is removed with the same rules as Del inside the S modal
  And settings are persisted
```
