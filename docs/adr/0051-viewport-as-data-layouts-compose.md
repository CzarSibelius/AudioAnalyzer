# ADR-0051: Viewport as data; layouts compose viewports; row renderer owns scroll state

**Status**: Accepted

## Context

Labeled UI rows (header Device/Now, BPM/Volume, toolbar Palette, settings hint) were built by passing data and labels into stateful `IScrollingTextViewport` instances at render time. Layouts did not "compose" viewports as data; they held multiple viewport instances and called `RenderWithLabel(label, text, ...)` with the right arguments each frame. This made it harder to define new layouts by simply listing which viewports (label + value source) appear in which order.

## Decision

1. **Viewport as data**: A **Viewport** is a simple type (label, optional hotkey, value getter `Func<IDisplayText>`, optional label/text colors). It holds no scroll state. Layouts compose UI by creating viewports (e.g. `new Viewport("Device", () => new PlainText(deviceName))`) and arranging them into rows.

2. **Row renderer**: **ILabeledRowRenderer** renders a single row of viewports into one line. It holds scroll state per slot (keyed by `startSlotIndex` + cell index) and uses `IScrollingTextEngine` for overflow. Given a list of viewports and widths, it invokes each getter, applies label formatting and scrolling, and concatenates cells. Different rows (header line 2, header line 3, toolbar, settings hint) use distinct slot ranges so scroll state is not shared.

3. **Layouts**: Header, toolbar, and settings modal build their rows as `IReadOnlyList<Viewport>` (and width lists) and call `rowRenderer.RenderRow(viewports, widths, totalWidth, palette, speed, startSlotIndex)`. No separate viewport instances are injected for each cell; the same `ILabeledRowRenderer` is used for all labeled rows.

4. **IScrollingTextViewport** remains for consumers that need a single stateful scrolling cell (e.g. TextLayersToolbarBuilder, which lives in **Application** as general UI, not in Visualizers). New layout code should prefer the Viewport + ILabeledRowRenderer pattern so layouts are data-driven.

## Consequences

- Header no longer receives four `IScrollingTextViewport` instances; HeaderDrawer builds row 2 and row 3 viewport lists and uses ILabeledRowRenderer.
- VisualizationPaneLayout and SettingsModalRenderer use ILabeledRowRenderer for the palette cell and hint line respectively.
- ADR-0037 (scrolling viewport) is still respected: scroll state lives in the row renderer and uses IScrollingTextEngine. The "viewport" as injectable renderer is used where a single cell is needed (e.g. toolbar builder); the data Viewport type is used for row-based layouts.
- The toolbar row is now built from **HorizontalRowComponent** with **ScrollingTextComponent** children (ADR-0056); viewport data is still composed the same way and set via SetRowData.
- References: [Viewport](../../src/AudioAnalyzer.Application/Abstractions/Viewport.cs), [ILabeledRowRenderer](../../src/AudioAnalyzer.Application/Abstractions/ILabeledRowRenderer.cs), [LabeledRowRenderer](../../src/AudioAnalyzer.Application/LabeledRowRenderer.cs), [ADR-0056](0056-scrolling-text-as-uicomponent.md).
