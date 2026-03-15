# ADR-0056: Scrolling text as IUiComponent with state

**Status**: Accepted

## Context

Scrolling text for dynamic UI (e.g. toolbar cells) was provided by **IScrollingTextViewport**: a stateful helper that produced label + scrolling content but was not part of the **IUiComponent** tree (ADR-0052, ADR-0053). The toolbar was a single **LabeledRowComponent** with viewports and per-slot scroll state on the row. We wanted scrolling text to be a first-class **IUiComponent** that owns its state, consistent with LabeledRowComponent and other components (ADR-0054).

## Decision

1. **ScrollingTextComponent**: Leaf **IUiComponent** that owns scroll state and last-text for content-change reset. Cell data (label, hotkey, colors, value getter, PreformattedAnsi) is set each frame via **SetFromDescriptor(LabeledValueDescriptor)** when used in a horizontal row. Rendered by **IUiComponentRenderer&lt;ScrollingTextComponent&gt;** (ScrollingTextComponentRenderer), which uses **IScrollingTextEngine** for scroll math and supports PreformattedAnsi (truncate-with-ellipsis or scroll). Returns **ComponentRenderResult.Line(1, line)** so a parent can collect content without writing.

2. **HorizontalRowComponent**: Leaf in the tree (GetChildren returns null). Holds a stable list of **ScrollingTextComponent** children and a list of widths. **SetRowData(descriptors, widths)** ensures enough children exist and sets each child from the corresponding descriptor each frame. Rendered by **HorizontalRowComponentRenderer**, which calls the ScrollingTextComponent renderer per cell with the allocated width, concatenates results, writes one line, returns **ComponentRenderResult.Written(1)**.

3. **Toolbar**: MainContentContainer builds the toolbar as a **HorizontalRowComponent** with ScrollingTextComponent children. Each frame it calls **SetRowData** with descriptors and widths from **BuildToolbarRowData** (same data source as before: visualizer GetToolbarViewports / GetToolbarSuffix). No separate **IUiStateUpdater** for the row; data is set in **GetMainComponents** before render.

4. **Label formatting**: Shared **LabelFormatting.FormatLabel(label, hotkey)** in Application.Display. Used by ScrollingTextComponentRenderer and by **TextLayersToolbarBuilder**. TextLayersToolbarBuilder no longer depends on **IScrollingTextViewport**; it uses LabelFormatting only.

5. **IScrollingTextViewport / factory**: Still registered for any future single-cell use outside the tree. Toolbar builder no longer uses them.

## Consequences

- Scrolling text is a proper UI component with state; toolbar row is built from HorizontalRowComponent and ScrollingTextComponent.
- LabeledRowComponent and ILabeledRowRenderer remain for header rows, modals, and any other row-based UI that composes LabeledValueDescriptor data; the non-tree path (RenderRow with startSlotIndex) is unchanged.
- PreformattedAnsi behavior is preserved for toolbar cells (e.g. first cell with ANSI layers string).
- References: [ScrollingTextComponent](../../src/AudioAnalyzer.Application/Abstractions/ScrollingTextComponent.cs), [ScrollingTextComponentRenderer](../../src/AudioAnalyzer.Application/ScrollingTextComponentRenderer.cs), [HorizontalRowComponent](../../src/AudioAnalyzer.Application/Abstractions/HorizontalRowComponent.cs), [HorizontalRowComponentRenderer](../../src/AudioAnalyzer.Console/Console/HorizontalRowComponentRenderer.cs), [LabelFormatting](../../src/AudioAnalyzer.Application/Display/LabelFormatting.cs), [ADR-0051](0051-viewport-as-data-layouts-compose.md), [ADR-0037](0037-scrolling-text-viewport-injectable-service.md), [ADR-0052](0052-ui-container-component-renderer.md).
