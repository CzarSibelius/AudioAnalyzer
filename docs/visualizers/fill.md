# Fill (layer)

## Description

Fills the entire viewport with a single color and a configurable fill character. Use low ZOrder to use it as a background (other layers draw on top), or high ZOrder to use it as an overlay. Color comes from the layer's palette (ColorIndex); the fill character can be a full block (█), half blocks (▀ ▄), shade characters (░ ▒ ▓), space, or a custom ASCII character.

## Snapshot usage

None. Fill does not use analysis snapshot data.

## Settings

- **Schema**: `TextLayerSettings` when `LayerType` is `Fill`; custom settings in `FillSettings`.
- **Color**: Use common **ColorIndex** and **PaletteId**. In the S modal, cycle Color index and use **P** to cycle the layer's palette.
- **Fill type** (enum, cycle in S modal): Which character to use for the fill.
  - **FullBlock** — Full block (█).
  - **HalfBlockUpper** — Upper half block (▀).
  - **HalfBlockLower** — Lower half block (▄).
  - **LightShade** — Light shade (░).
  - **MediumShade** — Medium shade (▒).
  - **DarkShade** — Dark shade (▓).
  - **Space** — Space.
  - **Custom** — Use the character from **Custom character**.
- **Custom character** (string, text-edit): Single character used when Fill type is Custom. Default `#`. Only the first character is used.
- **PaletteId** / **Enabled** / **ZOrder** / **BeatReaction** etc.: Standard common layer settings (Fill ignores beat reaction).

## Key bindings

- **1–9** — Select layer.
- **←/→** — Cycle layer type to Fill.
- **S** — Open settings; edit Fill type, Custom character, and common properties.
- **P** — Cycle palette for the active layer (when Fill is selected).

## Viewport constraints

- Uses full `ctx.Width` × `ctx.Height`. No minimum beyond the visualizer's (width ≥ 10, height ≥ 3).

## Implementation notes

- **Implementation**: `TextLayers/Fill/FillLayer.cs`, `FillSettings.cs`, `FillType.cs`.
- **State**: None; Fill is stateless. No per-layer state list in TextLayersVisualizer.
- **Draw**: Double loop over all cells; `ctx.Buffer.Set(x, y, fillChar, color)`. Color from `ctx.Palette[layer.ColorIndex % ctx.Palette.Count]`; fill character from FillType and CustomChar.
- **References**: [ADR-0014](../adr/0014-visualizers-as-layers.md), [ADR-0021](../adr/0021-textlayer-settings-common-custom.md).
