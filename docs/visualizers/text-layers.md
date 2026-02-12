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
  - `LayerType`: ScrollingColors, Marquee, FallingLetters, MatrixRain, WaveText, StaticText
  - `ZOrder`: int (lower = back)
  - `TextSnippets`: optional string array (text for marquee/wave/falling/matrix)
  - `BeatReaction`: None, SpeedBurst, Flash, SpawnMore, Pulse, ColorPop
  - `SpeedMultiplier`: double
  - `ColorIndex`: palette index

## Key bindings

- **P** — Cycle color palette (affects only Layered text; saved to its settings)
- **1–9** — Cycle the layer type for layers 1–9. Key 1 = layer 1 (back), key 9 = layer 9 (front). Number keys and numpad keys work. Changes persist to appsettings.json.
- Toolbar suffix: "Layers: N (1–9: cycle layer type)" or "Layers: (config in settings)" if empty

## Viewport constraints

- Minimum width: 10
- Minimum height: 3 lines
- Uses full `viewport.Width` × `viewport.MaxLines` for cell buffer
- If no layers configured, renders empty gray buffer

## Implementation notes

- **Internal state**: `ViewportCellBuffer`; `_layerStates` (offset, snippet index per layer); `_fallingLettersByLayer` (particles for FallingLetters); `_lastBeatCount`; `_beatFlashFrames`.
- **Cell buffer**: Per [ADR-0005](../adr/0005-layered-visualizer-cell-buffer.md); internal to this visualizer only.
- **Layer types**: Each type has its own class implementing `ITextLayerRenderer`: ScrollingColorsLayer (color grid), MarqueeLayer (scrolling text), FallingLettersLayer (falling particles), MatrixRainLayer (digital rain), WaveTextLayer (sinusoidal text), StaticTextLayer (centered static).
- **Beat reactions**: SpeedBurst (faster), Flash (advance/change), SpawnMore (spawn particles), Pulse (amplitude/color change), ColorPop (color offset).
- **References**: [ADR-0004](../adr/0004-visualizer-encapsulation.md), [ADR-0005](../adr/0005-layered-visualizer-cell-buffer.md).
