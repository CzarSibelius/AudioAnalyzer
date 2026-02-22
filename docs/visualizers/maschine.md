# Maschine (layer)

## Description

Beat-enabled text layer that displays one selected snippet in a diagonal cascade. On each beat a new line of the text is drawn below the previous; each line is offset one character to the left so the first character of the first line aligns with the second character of the second line, forming a stepped diagonal. Characters in the same column (the aligned diagonal) use a configurable accent color; the rest use the normal palette color. After enough beats to show one line per character of the snippet, the cycle loops and the snippet can advance to the next in the list. Uses the layer's palette and **TextSnippets**.

## Snapshot usage

- `BeatCount` — advances phase on each new beat; when phase wraps, cycle restarts and snippet may advance
- Palette — resolved per layer from `PaletteId` (or `TextLayers.PaletteId`); normal color from `ColorIndex`, accent from Custom `AccentColorIndex`

## Settings

- **Schema**: `TextLayerSettings` when `LayerType` is `Maschine`; layer-specific options in **Custom** (`MaschineSettings`).
- **TextSnippets** (common): List of text snippets; one is chosen per cycle (round-robin when the cycle loops).
- **ColorIndex** (common): Palette index for normal text color.
- **PaletteId** (common): Id of the color palette. **P** when this layer is selected cycles and saves.
- **Custom**:
  - **AccentColorIndex** (int, 0–31): Palette index for the aligned diagonal. Default 1.
  - **AccentColumnMode** (Fixed | Moving): Fixed = accent on the leftmost column of the cascade; Moving = accent column advances with beat phase so the highlight shifts each beat. Default Moving.

## Key bindings

- **1–9** — Select layer (when Maschine layer is selected, **P** cycles its palette)
- **←/→** — Cycle layer type to Maschine

## Viewport constraints

- Minimum width: 2
- Minimum height: 1 line
- Uses full `ctx.Width` × `ctx.Height`; the aligned (accent) diagonal is kept horizontally centered. The first line is fixed halfway up from vertical center (so the full cascade would be vertically centered); each new line appears below the previous without moving or scrolling existing lines
- Display width (columns) of the snippet is used for alignment and cycle length; wide characters count as 2 columns per [ADR-0039](../adr/0039-display-width-terminal-columns.md)

## Implementation notes

- **Implementation**: `TextLayers/Maschine/MaschineLayer.cs`
- **State**: `MaschineState` — last beat count, phase (0 .. text display length − 1), snippet index
- **Cycle**: Phase increments on each beat; when phase reaches the snippet’s display length it wraps to 0 and snippet index advances. Number of lines drawn = phase + 1.
- **References**: [ADR-0014](../adr/0014-visualizers-as-layers.md), [ADR-0021](../adr/0021-textlayer-settings-common-custom.md), [ADR-0039](../adr/0039-display-width-terminal-columns.md).
