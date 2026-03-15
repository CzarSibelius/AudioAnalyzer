# ADR-0051: Viewport as data; layouts compose viewports; row renderer owns scroll state

**Status**: Accepted

## Context

Labeled UI rows (header Device/Now, BPM/Volume, toolbar Palette, settings hint) were built by passing data and labels into stateful `IScrollingTextViewport` instances at render time. Layouts did not "compose" viewports as data; they held multiple viewport instances and called `RenderWithLabel(label, text, ...)` with the right arguments each frame. This made it harder to define new layouts by simply listing which viewports (label + value source) appear in which order.

## Decision

1. **Viewport as data**: A **Viewport** is a simple type (label, optional hotkey, value getter `Func<IDisplayText>`, optional label/text colors). It holds no scroll state. Layouts compose UI by creating viewports (e.g. `new Viewport("Device", () => new PlainText(deviceName))`) and arranging them into rows.

2. **Row components**: **HorizontalRowComponent** with **ScrollingTextComponent** children renders a single row of viewports into one line (ADR-0057). Each cell owns its scroll state; `IScrollingTextEngine` is used for overflow. Layouts set data each frame via `SetRowData(viewports, widths)` and `SetFromViewport` per child.

3. **Layouts**: Header, toolbar, and settings modal build their rows as `IReadOnlyList<Viewport>` (and width lists) and call `HorizontalRowComponent.SetRowData(viewports, widths)`. No separate viewport instances are injected for each cell; the same component type is used for all single-line rows.

4. **IScrollingTextViewport** remains for consumers that need a single stateful scrolling cell (e.g. TextLayersToolbarBuilder, which lives in **Application** as general UI, not in Visualizers). New layout code should prefer the Viewport + HorizontalRowComponent pattern so layouts are data-driven.

## Consequences

- Header builds row 2 and row 3 viewport lists and uses HorizontalRowComponent (ADR-0057).
- Settings modal hint line and toolbar are rendered via HorizontalRowComponent in the component tree.
- ADR-0037 (scrolling viewport) is still respected: scroll state lives on each ScrollingTextComponent and uses IScrollingTextEngine. The "viewport" as injectable renderer is used where a single cell is needed (e.g. toolbar builder); the data Viewport type is used for row-based layouts.
- The toolbar row is now built from **HorizontalRowComponent** with **ScrollingTextComponent** children (ADR-0056); viewport data is still composed the same way and set via SetRowData.
- References: [Viewport](../../src/AudioAnalyzer.Application/Abstractions/Viewport.cs), [HorizontalRowComponent](../../src/AudioAnalyzer.Application/Abstractions/HorizontalRowComponent.cs), [ADR-0056](0056-scrolling-text-as-uicomponent.md), [ADR-0057](0057-horizontal-row-unified-single-line-rows.md).
