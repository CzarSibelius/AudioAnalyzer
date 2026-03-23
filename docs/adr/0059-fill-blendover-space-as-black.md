# ADR-0059: Fill BlendOver — optional treat space cells as black for blending

**Status**: Accepted

## Context

`ViewportCellBuffer` stores both character and color per cell. After `Clear`, every cell is ASCII space (`' '`) with the same `PaletteColor` derived from the first sorted layer’s palette entry `[0]` (see `TextLayersVisualizer.Render`). Layers that `Set` pixels overwrite character and color.

Fill **BlendOver** (`FillCompositeMode.BlendOver`) reads **under** via `Get`, blends with `PaletteColorBlending.BlendOver`, then `Set`s the fill character. If **under** is the same RGB for all cells in a region (e.g. only the frame clear, no lower layer drew there), the result is a **uniform** tint. Users often expect gaps between shapes to **stay black** when applying a **black** dimming overlay, but the **clear** color may not be black (e.g. first palette entry is **DarkMagenta**). That outcome follows from **stored** buffer data, not from incorrect blend math.

A product direction is to treat **empty-looking** cells (conventionally **space**) **differently** for **BlendOver** only, so an opt-in dimming overlay can **lerp toward black** from **true black** in gaps instead of from the **clear** color.

## Decision

1. **Default behavior (unchanged)**  
   Fill **BlendOver** continues to use **both** `char` and `PaletteColor` from `ViewportCellBuffer.Get(x, y)` as **under** for blending. No change to `Clear`, Mirror, or other layers by default.

2. **Optional behavior**  
   **`FillSettings.BlendSpaceAsBlack`** (boolean, default false), editable in the S modal per [ADR-0023](0023-settings-modal-layer-editing.md) and discovered via reflection per [ADR-0025](0025-reflection-based-layer-settings.md). When **enabled** and the layer is in **BlendOver** mode:
   - If `Get` returns **`char == ' '`** (U+0020), treat **under** as **`PaletteColor.FromRgb(0, 0, 0)`** for the purpose of `PaletteColorBlending.BlendOver` only.
   - Otherwise, use the stored `PaletteColor` as today.

3. **Out of scope for this ADR**  
   Changing global `Clear` to a fixed black, appsettings-driven buffer default color for all layers, or other layers interpreting `Get` differently. Those would be separate decisions.

4. **Trade-off**  
   A layer that deliberately `Set`s **space** with a **non-black** color would, with this option enabled, still be blended **as if** under were black when Fill BlendOver runs on that cell. That is acceptable for an **opt-in** setting; document the risk in the Fill spec and setting description.

## Consequences

- **Implementation locus**: `FillLayer.Draw` only; branch before `BlendOver` when the setting is on and `c == ' '`.
- **Serialization**: New property on `FillSettings` inside layer `Custom`; per [ADR-0029](0029-no-settings-migration.md), missing property in old presets implies default **off**.
- **Documentation**: Update [docs/visualizers/fill.md](../visualizers/fill.md) when the setting ships.
- **Tests**: Cover BlendOver with a primed buffer: space + non-black color vs block character + magenta — output differs when the flag is on vs off.
- **Relations**: Complements [ADR-0058](0058-layer-render-bounds.md) (buffer clip on `Set`; `Get` remains unclipped). Does not change [ADR-0055](0055-layer-specific-beat-reaction.md) or layer state.
