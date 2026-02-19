# Mirror (layer)

## Description

Mirrors the current buffer content horizontally. One half of the screen is the source; the other half is overwritten with its mirror image. Place this layer **above** (higher Z-order than) the layers you want mirrored; it reads whatever has been drawn so far and writes the mirrored copy. Useful for symmetric effects (e.g. left half oscilloscope reflected on the right).

## Snapshot usage

None. Mirror only reads and copies the buffer; it does not use analysis snapshot data.

## Settings

- **Schema**: `TextLayerSettings` when `LayerType` is `Mirror`; custom settings in `MirrorSettings`.
- **Direction** (enum, cycle in S modal): Which half is the source.
  - **LeftToRight** — Left half is source; right half shows mirror of left.
  - **RightToLeft** — Right half is source; left half shows mirror of right.
- **PaletteId** / **Enabled** / **ZOrder** / **BeatReaction** etc.: Standard common layer settings (Mirror ignores palette and beat reaction).

## Key bindings

- **1–9** — Select layer.
- **←/→** — Cycle layer type to Mirror.
- **S** — Open settings; edit Direction and common properties.

## Viewport constraints

- Minimum width: 2 columns (mirror has no effect with 1 column).
- Uses full `ctx.Width` × `ctx.Height`; mirroring is by buffer column (left/right halves).
- Odd width: center column is unchanged for LeftToRight (source is `0 .. width/2-1`, destination `width/2 .. width-1`).

## Implementation notes

- **Implementation**: `TextLayers/Mirror/MirrorLayer.cs`, `MirrorSettings.cs`, `MirrorDirection.cs`.
- **State**: None; Mirror is stateless. No per-layer state list in TextLayersVisualizer.
- **Buffer read**: Uses `ViewportCellBuffer.Get(x, y)` to read cells, then `Set` to write the mirrored half.
- **References**: [ADR-0014](../adr/0014-visualizers-as-layers.md), [ADR-0021](../adr/0021-textlayer-settings-common-custom.md).
