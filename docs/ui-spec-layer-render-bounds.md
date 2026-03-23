# UI spec: Layer render bounds (visual edit)

Applies when the user starts **visual bounds editing** from the preset settings modal (**S**): select a layer, open the settings column, focus **Render region**, press **Enter**. The modal closes; the text-layer visualizer keeps rendering, and a **highlighted border** (box-drawing characters) is drawn on the **perimeter** of the layer’s current region using an accent color from that layer’s palette.

There are no extra console rows: the border overwrites cells on the edges of the region inside the same visualizer viewport as the composite.

## Screenshot

Capture during edit with **Ctrl+Shift+E** (see [ADR-0046](../adr/0046-screen-dump-ascii-screenshot.md)); the dump reflects the live composite including the border.

```text
(Paste screen dump here when the border is visible on your layout.)
```

## Line reference

Border cells are part of the visualizer grid (same row range as the normal layered view). Descriptions:

- **Top edge** — Horizontal line characters along the top of the region (or a single row of dashes if the region is one line tall).
- **Bottom edge** — Horizontal line along the bottom (when height ≥ 2).
- **Left/right edges** — Vertical line characters (or a single column if width is 1).
- **Corners** — `┌` `┐` `└` `┘` when width and height are at least 2.

If the region is a single cell, a `+` is used at that cell.
