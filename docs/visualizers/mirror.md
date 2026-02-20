# Mirror (layer)

## Description

Mirrors the current buffer content horizontally or vertically. One part of the screen is the source; the other part is overwritten with its mirror image. Place this layer **above** (higher Z-order than) the layers you want mirrored; it reads whatever has been drawn so far and writes the mirrored copy. Useful for symmetric effects (e.g. left half oscilloscope reflected on the right, or top half reflected to bottom).

## Snapshot usage

None. Mirror only reads and copies the buffer; it does not use analysis snapshot data.

## Settings

- **Schema**: `TextLayerSettings` when `LayerType` is `Mirror`; custom settings in `MirrorSettings`.
- **Direction** (enum, cycle in S modal): Which side is the source and mirror axis.
  - **LeftToRight** — Left side is source; right side shows mirror of left.
  - **RightToLeft** — Right side is source; left side shows mirror of right.
  - **TopToBottom** — Top side is source; bottom side shows mirror of top.
  - **BottomToTop** — Bottom side is source; top side shows mirror of bottom.
- **Mirror split %** (Split, int 25–75, cycle): Position of the mirror boundary as a percentage. Source and destination regions use the smaller of the two sides for 1:1 mirror (e.g. 50% = half and half; 25% = mirror the outer 25% on each end).
- **Rotation** (enum, cycle): Optional rotation of the mirrored (destination) region.
  - **None** — Mirrored content shown as-is.
  - **Flip180** — Flip the mirrored region 180° (both axes).
- **PaletteId** / **Enabled** / **ZOrder** / **BeatReaction** etc.: Standard common layer settings (Mirror ignores palette and beat reaction).

## Key bindings

- **1–9** — Select layer.
- **←/→** — Cycle layer type to Mirror.
- **S** — Open settings; edit Direction, Split %, Rotation, and common properties.

## Viewport constraints

- **Horizontal directions** (LeftToRight, RightToLeft): Minimum width 2 columns.
- **Vertical directions** (TopToBottom, BottomToTop): Minimum height 2 rows.
- Uses full `ctx.Width` × `ctx.Height`. Split percent and direction determine source and destination regions (1:1 mirror of the smaller side).

## Implementation notes

- **Implementation**: `TextLayers/Mirror/MirrorLayer.cs`, `MirrorSettings.cs`, `MirrorDirection.cs`, `MirrorRotation.cs`.
- **State**: None; Mirror is stateless. No per-layer state list in TextLayersVisualizer.
- **Buffer read**: Uses `ViewportCellBuffer.Get(x, y)` to read cells, then `Set` to write the mirrored region. Flip180 uses a temporary buffer to flip the destination rectangle in-place.
- **References**: [ADR-0014](../adr/0014-visualizers-as-layers.md), [ADR-0021](../adr/0021-textlayer-settings-common-custom.md).
