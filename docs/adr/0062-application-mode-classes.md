# ADR-0062: Application modes as `IApplicationMode` classes

**Status**: Accepted

## Context

Top-level modes (Preset editor, Show play, General settings) previously shared one main-content layout and a fixed three-row header. Mode-specific UI (toolbar cells, hub vs visualizer, header chrome) was scattered across `MainContentContainer`, `ApplicationShell`, and `VisualizationOrchestrator` (`if (ApplicationMode == Settings)` branches). We need a single place for mode behavior, optional per-mode toolbars, and different header density (e.g. Settings should not show Device/Now/BPM rows).

## Decision

1. **Persisted value**: Keep `ApplicationMode` enum on `VisualizerSettings` ([ADR-0029](0029-no-settings-migration.md)).

2. **Runtime**: Each mode is an **`IApplicationMode`** implementation (`PresetEditorApplicationMode`, `ShowPlayApplicationMode`, `SettingsApplicationMode`) registered in DI. **`IApplicationModeFactory`** returns the active implementation from `VisualizerSettings.ApplicationMode`.

3. **Ownership**: Each mode exposes **`HeaderLineCount`**, **`AllowsVisualizerFullscreen`**, **`UsesGeneralSettingsHubKeyHandling`**, **`TryHandleVisualizerKeys`**, and **`GetMainComponents`** (toolbar row + main leaf: `VisualizerAreaComponent` or `GeneralSettingsHubAreaComponent`). Shared toolbar helpers live in **`MainContentToolbarLayout`**.

4. **Orchestrator**: **`IApplicationModeHeaderProvider`** supplies header line count; **`VisualizationOrchestrator`** uses it for `VisualizationFrameContext.DisplayStartRow` each frame (with fullscreen and overlay unchanged). When a **fullscreen** layout is active and an **overlay** modal reserves top rows (`SetOverlayActive`), `DisplayStartRow` reflects the overlay height; **`MainContentContainer`** must keep that start row for the visualizer so it does not paint over the modal.

5. **Tab cycle**: **`IModeTransitionService`** (`ModeTransitionService`) centralizes Tab transitions and persistence (previously inline in `ApplicationShell`).

6. **Show toolbar**: **`TextLayersToolbarContext`** carries `ApplicationMode` and Show entry metadata; **`TextLayersToolbarBuilder`** builds a compact Show toolbar (Show name, Entry index, Palette) vs the full Preset editor toolbar. **`IShowPlayToolbarInfo`** (Console `ShowPlayToolbarInfo`) supplies entry index/count without referencing Console types from Visualizers.

## Consequences

- New types in Console: `IApplicationMode`, factory, three mode classes, `MainContentToolbarLayout`, `MainContentRenderArgs`, `ModeTransitionService`, `ShowPlayToolbarInfo`.
- Application: `IShowPlayToolbarInfo`, extended `TextLayersToolbarContext`.
- `IVisualizationOrchestrator.SetHeaderCallback` no longer takes a fixed header row count.
- Documentation: General settings hub spec updated (title-only header in Settings); [ADR-0061](0061-general-settings-mode.md) clarified for header rows.

## References

- `ApplicationShell`, `MainContentContainer`, `HeaderContainer`, `VisualizationOrchestrator`
- `TextLayersVisualizer`, `TextLayersToolbarBuilder`
