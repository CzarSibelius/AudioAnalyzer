# ADR-0054: UI component state ownership and IUiStateUpdater

**Status**: Accepted

## Context

ADR-0052 introduced the IUiComponent tree and IUiComponentRenderer. State was split: header data (device, BPM, volume) lived in the container (HeaderContainer), and scroll state for labeled rows lived in the renderer (LabeledRowRenderer) keyed by slot index. Components were recreated each frame (ephemeral tree), so they could not own state. We wanted components to own their state and a clear update contract before render.

## Decision

1. **Components own their state.** UI component instances that need state (e.g. scroll position, header data) hold it. Stateful components are **stable** across frames: containers hold the same child component instances and return them from GetChildren after updating their data (e.g. via SetRowData on LabeledRowComponent).

2. **Update contract: IUiStateUpdater&lt;TComponent&gt;.** State is updated by a separate updater type, not by a method on the component. The interface is in Application.Abstractions: `void Update(TComponent component, RenderContext context)`. This mirrors the IUiComponentRenderer pattern (dispatch by type; component holds data, updater applies context).

3. **Dispatcher: IUiStateUpdater&lt;IUiComponent&gt;.** A single entry point updates the tree: for composites it recurses to children; for stateful leaves it resolves the concrete updater (e.g. IUiStateUpdater&lt;HeaderContainer&gt;) and calls it. Stateless components have no updater registered. The dispatcher is called **before** IUiComponentRenderer.Render so component state is current when rendering.

4. **Scroll state on LabeledRowComponent.** Per-slot scroll state (ScrollingTextViewportState and last text for content-change detection) lives on the component instance. LabeledRowComponent has GetSlotState(index) and owns a list of LabeledRowSlotState. The renderer uses the component’s state when rendering via Render(LabeledRowComponent, context). The non-tree path (ILabeledRowRenderer.RenderRow with startSlotIndex) still uses the renderer’s internal slot state for backward compatibility.

5. **Stable tree where state lives.** HeaderContainer holds three stable LabeledRowComponent instances (_titleRow, _row2, _row3) and in GetChildren sets their row data via SetRowData and returns them. MainContentContainer holds a stable _toolbarRow and returns it (with SetRowData) plus VisualizerAreaComponent.Instance from the root composite’s getter. So scroll state persists because the same row component instance is reused each frame.

6. **Header state via HeaderContainerStateUpdater.** HeaderContainer holds device name, BPM, volume, now-playing, etc. It does not update that state in GetChildren. Instead, HeaderContainerStateUpdater (IUiStateUpdater&lt;HeaderContainer&gt;) reads from RenderContext and services (INowPlayingProvider, AnalysisEngine), builds HeaderStateData, and calls header.ApplyState(data). The updater runs as part of the dispatcher before render.

## Consequences

- Components own their state; updaters are separate types registered in DI. New stateful component types add a concrete updater and register it; the dispatcher is extended to call it (e.g. by type switch).
- Render loop order: UpdateState(root, context) then Render(root, context). Call sites (HeaderContainer.DrawMain/DrawHeaderOnly, MainContentContainer.Render) call the state updater then the component renderer.
- LabeledRowComponent supports both stable (SetRowData) and one-off (constructor) use; scroll state is per component instance.
- ADR-0051’s “row renderer owns scroll state” is refined: when rendering through the component tree, the **component** owns scroll state; the renderer reads/writes it on the component. RenderRow (non-tree) still uses the renderer’s internal slots.
- References: [IUiStateUpdater](../../src/AudioAnalyzer.Application/Abstractions/IUiStateUpdater.cs), [UiComponentStateUpdater](../../src/AudioAnalyzer.Console/Console/UiComponentStateUpdater.cs), [HeaderContainerStateUpdater](../../src/AudioAnalyzer.Console/Console/HeaderContainerStateUpdater.cs), [LabeledRowComponent](../../src/AudioAnalyzer.Application/Abstractions/LabeledRowComponent.cs), [LabeledRowSlotState](../../src/AudioAnalyzer.Application/Abstractions/LabeledRowSlotState.cs), [ADR-0052](0052-ui-container-component-renderer.md), [ADR-0053](0053-iuicomponent-all-ui.md).
