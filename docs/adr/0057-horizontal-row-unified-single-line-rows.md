# ADR-0057: HorizontalRowComponent for all single-line viewport rows

**Status**: Accepted

## Context

Single-line UI rows (header title, header Device/Now and BPM/Volume, toolbar, settings modal hint) were implemented in two ways: **LabeledRowComponent** with **ILabeledRowRenderer** (header and modal hint), and **HorizontalRowComponent** with **ScrollingTextComponent** children (toolbar only, ADR-0056). Both accept the same data (Viewport list + widths) and produce equivalent output (label + scrolling/truncation per cell). Maintaining two row abstractions added complexity and duplicated behavior.

## Decision

1. **All single-line viewport rows use HorizontalRowComponent.** Header (title row, row 2, row 3) and the settings modal hint line are implemented as **HorizontalRowComponent** with **ScrollingTextComponent** children. Data is set each frame via **SetRowData(viewports, widths)**; each child is updated via **SetFromViewport**. Rendered by **HorizontalRowComponentRenderer** and **ScrollingTextComponentRenderer**.
2. **LabeledRowComponent and ILabeledRowRenderer are removed.** They are no longer used. Viewport data and layout semantics are unchanged; only the component type and renderer are unified.
3. **Settings modal hint is in the component tree.** The settings overlay draws the hint line by building a small **IUiComponent** tree (CompositeComponent with one HorizontalRowComponent), setting the hint row data, and calling **IUiComponentRenderer.Render** with an overlay-scoped RenderContext (ADR-0053). No standalone **ILabeledRowRenderer.RenderRow** call.

## Consequences

- One row component type for all viewport-based single-line rows; simpler dispatcher and fewer types.
- Header and settings modal use the same rendering path as the toolbar (HorizontalRowComponent + ScrollingTextComponent).
- **ILabeledRowRenderer**, **LabeledRowRenderer**, **LabeledRowComponent**, and **LabeledRowSlotState** have been removed from the codebase.
- References: [HorizontalRowComponent](../../src/AudioAnalyzer.Application/Abstractions/HorizontalRowComponent.cs), [ScrollingTextComponent](../../src/AudioAnalyzer.Application/Abstractions/ScrollingTextComponent.cs), [ADR-0053](0053-iuicomponent-all-ui.md), [ADR-0056](0056-scrolling-text-as-uicomponent.md).