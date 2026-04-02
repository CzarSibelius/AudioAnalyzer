# ADR-0070: Add and remove text layers in the settings modal

**Status**: Accepted

## Context

Users want presets with **fewer than** `TextLayersLimits.MaxLayerCount` layers without keeping unused “padding” layers, and the ability to **add** layers back up to that cap. [ADR-0023](0023-settings-modal-layer-editing.md) makes the **S** preset/layer settings modal the canonical place for **editing properties** of existing layers; it does not define **structural** changes to the layer list.

Today, [FileSettingsRepository](../../src/AudioAnalyzer.Infrastructure/FileSettingsRepository.cs) pads `TextLayers.Layers` to `MaxLayerCount` on load so digit keys always map to nine entries. That conflicts with persisting a genuinely shorter list. Digit keys and the toolbar already tolerate fewer layers when the list is shorter in memory ([TextLayersKeyHandler](../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayersKeyHandler.cs), [TextLayersToolbarBuilder](../../src/AudioAnalyzer.Application/TextLayersToolbarBuilder.cs)).

The **upper** bound remains a single code constant per [ADR-0045](0045-max-text-layers-constant.md) (not user-editable in appsettings). [ADR-0019](0019-preset-textlayers-configuration.md) describes presets as holding a `TextLayersVisualizerSettings` snapshot; the **count** of layers in that snapshot may vary once this decision is implemented.

## Decision

1. **Canonical UI**: **Add layer** and **remove layer** are exposed in the **S** modal (same surface as layer property editing), not as ad-hoc global shortcuts outside the modal. Concrete keys or rows are chosen at implementation time but must satisfy [ADR-0048](0048-key-handlers-expose-bindings.md) and appear in dynamic help per [ADR-0049](0049-dynamic-help-screen.md).

2. **Count bounds**: `0 <= Layers.Count <= TextLayersLimits.MaxLayerCount`. **Add** at `MaxLayerCount` is a no-op or disabled in the UI. **Remove** at `0` layers is a no-op or disabled. Allowing **zero** layers means the main visualization area may show no text layers; implementers must define safe behavior (empty buffer, messaging, modal navigation) and audit paths that assumed `Count > 0`.

3. **Defaults for new layers**: New layers use the same default construction approach as existing factory helpers (e.g. `IDefaultTextLayersSettingsFactory`, including the pattern used for padding layers). The exact layer type/ZOrder for a new row is an implementation detail as long as it is consistent and persisted.

4. **Persistence**: Active preset `Config` and live `TextLayers` serialize the **actual** `Layers` list length. After this behavior ships, **do not** unconditionally pad to `MaxLayerCount` on load. Still **cap** lists longer than `MaxLayerCount` when loading (per ADR-0045). This is a behavior change, not a schema migration with transform logic; align with [ADR-0029](0029-no-settings-migration.md) (no migration code—users with incompatible expectations rely on backup/reset policy if needed).

5. **State and selection**: On remove (and when indices shift), clear or invalidate per-layer animation state as required by [ADR-0043](0043-textlayer-state-store.md). Clamp settings-modal `SelectedLayerIndex`, palette-cycle index (`PaletteCycleLayerIndex` or equivalent), and title-bar navigation fields so they remain valid for the new list length.

6. **Relationship to ADR-0023**: Layer list management (**add/remove**) is part of the same **S** modal workflow as property editing: implement in `SettingsModal` / `ISettingsModalRenderer` / `IKeyHandler<SettingsModalKeyContext>` (and shared helpers), not in `TextLayersVisualizer.HandleKey` as the primary entry point.

## Consequences

- **Infrastructure**: Replace or narrow `EnsureTextLayersHasNineLayers`-style padding so saved presets can retain fewer than `MaxLayerCount` layers; keep max cap for oversized JSON.
- **Console**: Settings modal renderer and key handler gain add/remove flows; hint row and [ui-spec-preset-settings-modal.md](../ui-spec-preset-settings-modal.md) should be updated when the feature ships.
- **Application / Visualizers**: Optional factory API for “create new layer” if not already sufficient; audit TextLayers draw and toolbar/help for `Layers.Count == 0`.
- **Tests**: Load/save round-trip and modal behavior for 0…`MaxLayerCount` layers.
- **Cross-references**: [ADR-0023](0023-settings-modal-layer-editing.md), [ADR-0045](0045-max-text-layers-constant.md), [ADR-0019](0019-preset-textlayers-configuration.md), [ADR-0043](0043-textlayer-state-store.md), [ADR-0029](0029-no-settings-migration.md).
