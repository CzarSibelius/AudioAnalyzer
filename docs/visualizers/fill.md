# Fill (layer)

## Description

Fills the layer’s **Render region** (or the full viewport when unset) with a solid or gradient color and a configurable fill character. Use low ZOrder to use it as a background (other layers draw on top), or high ZOrder to use it as an overlay—including **Blend over** mode to darken or tint what is already drawn (e.g. black at 50% blend strength). Colors come from the layer palette; blending mixes in **24-bit RGB** (16-color palette entries use a fixed VGA-style RGB table).

## Snapshot usage

None. Fill does not use analysis snapshot data.

## Settings

- **Schema**: `TextLayerSettings` when `LayerType` is `Fill`; custom settings in `FillSettings`.
- **Color**: Use common **ColorIndex** (gradient **start** when using gradient) and **PaletteId**. In the S modal, cycle Color index and use **P** to cycle the layer's palette.
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
- **Color style** (`FillColorStyle`): **Solid** — single color from **Color index**; **Gradient** — linear blend between **Color index** (start) and **Gradient end color index** (end), modulo palette size.
- **Gradient end color index** (0–99, cycle): End of the gradient; ignored when color style is Solid.
- **Gradient direction** (`FillGradientDirection`): Axis for the gradient—four cardinals (e.g. left→right, top→bottom) and four diagonals between viewport corners.
- **Composite** (`FillCompositeMode`): **Replace** — overwrite cells (default). **BlendOver** — mix the fill color over the existing cell color from lower Z-order layers (see **Blend strength**).
- **Blend strength** (0.0–1.0, step 0.05): Used only when composite is **BlendOver**. `0` leaves the buffer unchanged for that cell; `1` matches **Replace** for color. Example: set fill to black (via palette / Color index), composite **BlendOver**, strength **0.5** to dim the area under the fill.
- **Blend space as black** (bool, cycle in S modal): Only applies when composite is **BlendOver**. When **on**, cells whose character is still ASCII space (`' '`) use **RGB black** as the **under** color for blending (not the stored clear color). Default **off**. See [ADR-0059](../adr/0059-fill-blendover-space-as-black.md) for rationale and trade-off (deliberate space + non-black color is still blended as if under were black).
- **PaletteId** / **Enabled** / **ZOrder** / **Render region** etc.: Standard common layer settings. Fill does not support beat reaction (no BeatReaction setting).

## Troubleshooting (Blend over looks like a flat slab)

1. **Composite mode**: Default is **Replace**, which paints a solid color. For dimming/tinting what is already drawn, set **Composite** to **BlendOver** in the S modal. **Replace** + low **Blend strength** still behaves like **Replace** for color (strength only applies to **BlendOver**).

2. **Uniform color across the whole Fill region**: **BlendOver** blends per cell: `out = lerp(under, fillRgb, strength)` using the existing cell color **under** (`buffer.Get`). If every cell in your **Render region** has the **same** underlying RGB (e.g. nothing drew there except the initial clear, or the same clear color everywhere), the result is **one flat color**—not a bug in the blend.

3. **What “empty” cells are**: The buffer is cleared each frame with the **first color** of the **first layer’s** palette (see `TextLayersVisualizer.Render` → `ViewportCellBuffer.Clear`). Gaps between shapes are **not** automatically true black; they are that clear color. BlendOver darkens **that** toward your fill color. For black gaps under a black-tint overlay, set the **first** layer’s palette so index **0** is **#000000** (or `Black`), use a shipped palette such as **clear-black** (see `palettes/clear-black.json`), **or** enable **Blend space as black** on the Fill overlay (see below).

4. **Overlap with lower layers**: To dim the **bottom** of bars or other art, those layers must **draw into the same cells** as your Fill **Render region** (same viewport area), and the Fill layer must have a **higher ZOrder** (drawn later) than those layers. If lower layers only paint the top half of the viewport, the bottom half only has the clear color—BlendOver will look uniform there.

5. **“Black” fill**: **Color index** must map to actual black in RGB (or VGA black for 16-color entries). Palettes like **monochrome** use `#1A1A1A` at index 0, not `#000000`.

6. **Example preset**: `presets/fill-blendover-demo.json` (shipped next to the executable) demonstrates **clear-black** as the first layer palette, a full-viewport black **Replace** base, **Oscilloscope** on top, and a bottom-half **BlendOver** overlay with **Blend space as black** enabled on that overlay. Press **V** until that preset is active.

7. **Blend space as black** ([ADR-0059](../adr/0059-fill-blendover-space-as-black.md)): The **Blend space as black** Fill setting (S modal) treats **space** (`' '`) cells as **black RGB** for **BlendOver** blending only, so gaps can stay visually black under a black dimming overlay without changing the first-layer palette. Default **off**; enable when you want that behavior and accept the documented trade-off (intentional space + non-black color still blends as if under were black).

## Key bindings

- **1–9** — Select layer.
- **←/→** — Cycle layer type to Fill.
- **S** — Open settings; edit Fill custom properties and common properties.
- **P** — Cycle palette for the active layer (when Fill is selected).

## Viewport constraints

- Uses full `ctx.Width` × `ctx.Height` (or the layer’s **Render region** when set). No minimum beyond the visualizer’s (width ≥ 10, height ≥ 3).

## Implementation notes

- **Implementation**: `TextLayers/Fill/FillLayer.cs`, `FillSettings.cs`, `FillType.cs`, `FillColorStyle.cs`, `FillGradientDirection.cs`, `FillCompositeMode.cs`.
- **Blending**: `AudioAnalyzer.Application.Display.PaletteColorBlending` — `ToRgb`, `LerpRgb`, `BlendOver` (console colors mapped to approximate RGB for mixing).
- **State**: None; Fill is stateless. No per-layer state list in TextLayersVisualizer.
- **Draw**: `TextLayerRenderBounds.ToPixelRect` defines the filled rectangle; only those cells are visited (matches clip from TextLayersVisualizer). Gradient `t` uses **local** coordinates within that rectangle via `FillLayer.ComputeGradientT` (projection on segment for diagonals). **Replace**: `Set(fillChar, color)`. **BlendOver**: `Get` then optionally substitute black as **under** when **Blend space as black** is on and `char == ' '`; then `BlendOver(under, fill, strength)` then `Set` with the same fill character. The frame’s initial clear color comes from the first sorted layer’s palette entry `[0]` (not a fixed black)—see **Troubleshooting** above and `TextLayersVisualizer.Render`.
- **References**: [ADR-0014](../adr/0014-visualizers-as-layers.md), [ADR-0021](../adr/0021-textlayer-settings-common-custom.md), [ADR-0058](../adr/0058-layer-render-bounds.md), [ADR-0059](../adr/0059-fill-blendover-space-as-black.md).
