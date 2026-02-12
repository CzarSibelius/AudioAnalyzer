# Layered text (textlayers)

## Description

Composites multiple independent layers (e.g. ScrollingColors, Marquee, FallingLetters) with configurable text snippets and beat-reactive behavior. Uses a viewport-sized cell buffer for z-order compositing; layers are drawn in ascending ZOrder (lower = back).

## Snapshot usage

- `TextLayersConfig` — layer list and per-layer settings (set by renderer when mode is TextLayers)
- `Palette` — color palette for layers (fallback to default if empty)
- `BeatCount` — used for beat reactions
- `BeatFlashActive` — triggers SpeedBurst, Flash, SpawnMore, Pulse, ColorPop when true

## Settings

- **Schema**: `VisualizerSettings.TextLayers`
- **PaletteId** (string, optional): Id of the selected color palette (e.g. `"default"`). P key cycles and saves to this setting.
- **Layers** (array): Each layer has:
  - `LayerType`: None, ScrollingColors, Marquee, FallingLetters, MatrixRain, WaveText, StaticText, AsciiImage
  - `ZOrder`: int (lower = back)
  - `TextSnippets`: optional string array (text for marquee/wave/falling/matrix)
  - `BeatReaction`: None, SpeedBurst, Flash, SpawnMore, Pulse, ColorPop
  - `SpeedMultiplier`: double
  - `ColorIndex`: palette index
  - `ImageFolderPath`: optional string (path to folder with images; for AsciiImage only)
  - `AsciiImageMovement`: None, Scroll, Zoom, Both (for AsciiImage; default Scroll)

## Key bindings

- **P** — Cycle color palette (affects only Layered text; saved to its settings)
- **1–9** — Cycle the layer type for layers 1–9. Key 1 = layer 1 (back), key 9 = layer 9 (front). Number keys and numpad keys work. Changes persist to appsettings.json.
- **Shift+1–9** — Set the corresponding layer to None (invisible). Changes persist to appsettings.json.
- **I** — Cycle to the next picture in AsciiImage layers (only when at least one layer is AsciiImage).
- Toolbar suffix: "Layers: N (1–9: cycle, Shift+1–9: None, I: next image)" when AsciiImage layers exist; otherwise "Layers: N (1–9: cycle, Shift+1–9: None)" or "Layers: (config in settings)" if empty

## Viewport constraints

- Minimum width: 10
- Minimum height: 3 lines
- Uses full `viewport.Width` × `viewport.MaxLines` for cell buffer
- If no layers configured, renders empty gray buffer

## Implementation notes

- **Internal state**: `ViewportCellBuffer`; `_layerStates` (offset, snippet index per layer); `_fallingLettersByLayer` (particles for FallingLetters); `_asciiImageStateByLayer` (scroll, zoom, cache for AsciiImage); `_lastBeatCount`; `_beatFlashFrames`.
- **Cell buffer**: Per [ADR-0005](../adr/0005-layered-visualizer-cell-buffer.md); internal to this visualizer only.
- **None layer**: `LayerType.None` renders nothing; no renderer registered. Use Shift+1–9 to set a layer to None.
- **Layer types**: Each type has its own class implementing `ITextLayerRenderer`, in a per-layer subfolder: `ScrollingColors/ScrollingColorsLayer`, `Marquee/MarqueeLayer`, `FallingLetters/FallingLettersLayer`, `MatrixRain/MatrixRainLayer`, `WaveText/WaveTextLayer`, `StaticText/StaticTextLayer`, `AsciiImage/AsciiImageLayer`. Shared infrastructure (ITextLayerRenderer, TextLayerSettings, TextLayerDrawContext, etc.) stays in the TextLayers root.
- **AsciiImage layer**: Reads images (BMP, GIF, JPEG, PNG, WebP) from `ImageFolderPath`, converts to ASCII via grayscale-to-character mapping; supports scroll, zoom, or both via `AsciiImageMovement`; uses palette gradient for colored output; Flash beat reaction cycles to next image. Depends on SixLabors.ImageSharp.
- **Beat reactions**: SpeedBurst (faster), Flash (advance/change), SpawnMore (spawn particles), Pulse (amplitude/color change), ColorPop (color offset).
- **References**: [ADR-0004](../adr/0004-visualizer-encapsulation.md), [ADR-0005](../adr/0005-layered-visualizer-cell-buffer.md).
