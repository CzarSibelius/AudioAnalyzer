# ADR-0052: UI container, component, and generic component renderer

**Status**: Accepted

## Context

The console UI was built from separate renderers (IHeaderDrawer, IVisualizationRenderer) and static helpers (ConsoleHeader) that knew how to draw specific regions. Layouts composed viewport data and called ILabeledRowRenderer directly. There was no unified pattern for "a container that owns a set of UI parts and renders them by looping over components," or for "a single renderer that dispatches to the right concrete renderer by component type."

## Decision

1. **IUiComponent (unified tree)**: A node in the UI tree. It exposes `GetChildren(RenderContext)`. When **null or empty**, the component is a leaf: it carries only the data its renderer needs. The leaf types are **HorizontalRowComponent** (all single-line rows: title bar, header rows 2–3, toolbar, settings modal hint; ADR-0057), **ScrollingTextComponent** (cell child of HorizontalRowComponent), and **VisualizerAreaComponent** (block region). When **non-empty**, the component is a composite: the renderer recurses over children instead of dispatching by type. Leaf components return null from `GetChildren`; composites use `CompositeComponent`, which holds a list or a getter and returns it from `GetChildren`.

2. **CompositeComponent**: Concrete composite that holds a list of children (or a `Func<RenderContext, IReadOnlyList<IUiComponent>>` for dynamic children). The renderer does not dispatch it to a leaf renderer; it calls `GetChildren(context)` and then `Render(child, context)` for each child, advancing `context.StartRow`.

3. **RenderContext**: Passed from the caller to the renderer. Supplies layout (Width, StartRow, MaxLines, Palette, ScrollSpeed) and optional data (DeviceName for header, Snapshot and PaletteDisplayName for main content). The renderer advances `StartRow` after each component when recursing. Optional `InvalidateWriteCache` tells the renderer to clear any line caches (e.g. header write-if-changed).

4. **IUiComponentRenderer (generic dispatcher)**: Single entry point `Render(IUiComponent component, RenderContext context)`. If `component.GetChildren(context)` is non-null and non-empty, the implementation recurses: for each child it calls `Render(child, context)` and advances `context.StartRow`. Otherwise it selects the concrete renderer by the component's runtime type: **HorizontalRowComponent** → HorizontalRowComponentRenderer, **VisualizerAreaComponent** → visualizer area renderer. Callers depend only on this interface. Optional `ResetVisualizerAreaCleared()` for clearing the visualizer region on next paint (e.g. when leaving fullscreen).

5. **Header and main content**: The header is implemented as `IHeaderContainer` (DrawMain, DrawHeaderOnly). Its implementation builds a root `CompositeComponent` whose children are three **HorizontalRowComponent** instances (title row from ITitleBarContentProvider, row 2: Device/Now, row 3: BPM/Volume), builds `RenderContext`, and calls `IUiComponentRenderer.Render(root, context)`. The main content is implemented as `IVisualizationRenderer`; its implementation builds a root `CompositeComponent` (one **HorizontalRowComponent** for the toolbar + **VisualizerAreaComponent**, or only visualizer area in fullscreen) and calls the renderer. The shell and orchestrator use IHeaderContainer and IVisualizationRenderer; the unified component tree is the way to implement those contracts.

6. **ConsoleDimensions**: Static helper `ConsoleDimensions.GetConsoleWidth()` replaces the former `ConsoleHeader.GetConsoleWidth()` for use in modals and other code that does not have IDisplayDimensions injected.

## Consequences

- The UI is a single tree of IUiComponent nodes; leaves return null/empty from GetChildren, composites return a list. One abstraction, one place for the render loop (inside the dispatcher).
- New UI regions add a root CompositeComponent and call the renderer; new leaf types extend the dispatcher's switch. Nested layouts are supported by adding more composite nodes.
- Callers stay unaware of which concrete renderer draws each leaf; the dispatcher holds that mapping.
- All single-line UI (title bar, header rows, toolbar, settings modal hint) uses **HorizontalRowComponent** with **ScrollingTextComponent** children (ADR-0057); only the block region (VisualizerAreaComponent) has a dedicated renderer. The "generic" layer is the row component plus the dispatcher.
- ADR-0051 is preserved: Viewport remains data; rows use HorizontalRowComponent with viewport lists.
- IHeaderDrawer, HeaderDrawer, ConsoleHeader, and VisualizationPaneLayout are removed; IHeaderContainer, HeaderContainer, MainContentContainer, and UiComponentRenderer implement the pattern. IUiContainer and UiContainerBase were removed in favor of the unified IUiComponent tree and CompositeComponent.
- References: [IUiComponent](../../src/AudioAnalyzer.Application/Abstractions/IUiComponent.cs), [CompositeComponent](../../src/AudioAnalyzer.Application/Abstractions/CompositeComponent.cs), [IUiComponentRenderer](../../src/AudioAnalyzer.Application/Abstractions/IUiComponentRenderer.cs), [RenderContext](../../src/AudioAnalyzer.Application/Abstractions/RenderContext.cs), [HeaderContainer](../../src/AudioAnalyzer.Console/Console/HeaderContainer.cs), [MainContentContainer](../../src/AudioAnalyzer.Console/Console/MainContentContainer.cs), [UiComponentRenderer](../../src/AudioAnalyzer.Console/Console/UiComponentRenderer.cs).
