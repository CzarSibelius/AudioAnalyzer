# ASCII Image (layer only)

## Description

Renders images from a configured folder as ASCII art. Converts each image to a character grid using a grayscale-to-character gradient; supports scroll, zoom, and configurable movement. This layer type is part of TextLayersVisualizer; there is no standalone ASCII image visualizer.

## Snapshot usage

- Uses `BeatFlashActive` for Flash beat reaction (cycle to next image) and SpeedBurst (faster movement)
- No other snapshot fields; layer is self-contained

## Settings

- **Schema**: `TextLayerSettings` when `LayerType == AsciiImage`; custom settings in `AsciiImageSettings`
- **BeatReaction** (enum, default: None): How a beat affects the layer. **None**, **SpeedBurst** (faster movement), **Flash** (cycle to next image), **Pulse** (color step on beat). Edited in S modal.
- **ImageFolderPath** (string, optional): Path to folder containing images (BMP, GIF, JPEG, PNG, WebP). Sorted alphabetically. When empty, the effective folder is the **global default asset base** from General settings (`UiSettings.DefaultAssetFolderPath` in `appsettings.json`), or **`AppContext.BaseDirectory`** when that setting is unset. Relative paths are resolved under that global base; absolute paths ignore the global base.
- **SelectedImageFileName** (string, optional, persisted in preset `Custom`): File name (not full path) of the image to show within `ImageFolderPath`. When null or missing, the first file in sorted order is used. **I** and **Flash** beat reaction advance this value; it is saved with the preset when another handled key triggers save (same as other layer custom fields). Not shown in the S modal.
- **Movement** (enum, default: Scroll): None, Scroll, Zoom, or Both
- **PaletteSource** (enum, default: LayerPalette): LayerPalette (map brightness to layer's palette) or ImageColors (use per-pixel RGB from image)
- **ZoomMin** (double, default: 0.85): Minimum zoom scale (0.5–1.0)
- **ZoomMax** (double, default: 1.3): Maximum zoom scale (1.0–2.0)
- **ZoomSpeed** (double, default: 0.02): Multiplier for zoom phase increment (0.005–0.1)
- **ZoomStyle** (enum, default: Sine): Sine (sinusoidal), Breathe (ease-in-out), PingPong (linear ramp)
- **ScrollRatioY** (double, default: 0.5): ScrollY = ScrollX × this (0 = horizontal only, 1 = diagonal equal)
- **PaletteId** (string, optional): Used when PaletteSource is LayerPalette; inherited from TextLayers when null

## Key bindings

- **I** — Cycle to next picture (when at least one AsciiImage or AsciiModel layer exists); updates persisted selection in the preset
- **S** — Open preset modal to edit all layer settings including AsciiImage options

## Viewport constraints

- Uses full `viewport.Width` × `viewport.Height` (cell buffer)
- Converts at 2× size internally to allow scroll and zoom headroom
- No minimum; layer composites with others

## Implementation notes

- **Layer**: `AsciiImageLayer` in `TextLayers/AsciiImage/AsciiImageLayer.cs`
- **Converter**: `AsciiImageConverter.Convert` loads image via SixLabors.ImageSharp, resizes (preserve aspect), outputs chars + brightness; when `includeRgb` true, also outputs per-pixel R,G,B
- **State**: `AsciiImageState` holds ScrollX, ScrollY, ZoomPhase, cached frame; cache invalidated when path, dimensions, or PaletteSource change
- **PaletteSource=LayerPalette**: Maps brightness (0–255) to palette index; uses `ctx.Palette`, ColorIndex, Pulse beat reaction
- **PaletteSource=ImageColors**: Uses `PaletteColor.FromRgb(r,g,b)` from cached frame; no palette lookup
- **References**: [text-layers.md](text-layers.md), [ADR-0014](../adr/0014-visualizers-as-layers.md).
