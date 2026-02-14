# Layered text (textlayers)

## Description

Composites multiple independent layers (e.g. ScrollingColors, Marquee, FallingLetters) with configurable text snippets and beat-reactive behavior. Uses a viewport-sized cell buffer for z-order compositing; layers are drawn in ascending ZOrder (lower = back).

## Snapshot usage

- `TextLayersConfig` — layer list and per-layer settings (set by renderer when mode is TextLayers)
- Each layer resolves its own palette from `PaletteId` (or inherits from `TextLayers.PaletteId`); no shared snapshot palette
- `BeatCount` — used for beat reactions
- `BeatFlashActive` — triggers SpeedBurst, Flash, SpawnMore, Pulse, ColorPop when true
- `SmoothedMagnitudes`, `TargetMaxMagnitude` — used by GeissBackground and BeatCircles for bass/treble intensity and plasma modulation
- `Waveform`, `WaveformPosition`, `WaveformSize` — used by Oscilloscope layer for time-domain waveform

## Settings

- **Schema**: `VisualizerSettings.TextLayers`
- **PaletteId** (string, optional): Default palette id for layers that do not have their own. Fallback when a layer's `PaletteId` is null/empty.
- **Layers** (array): Each layer has:
  - `LayerType`: None, ScrollingColors, Marquee, FallingLetters, MatrixRain, WaveText, StaticText, AsciiImage, GeissBackground, BeatCircles, Oscilloscope
  - `Enabled`: bool (default true; when false, layer is not rendered)
  - `ZOrder`: int (lower = back)
  - `TextSnippets`: optional string array (text for marquee/wave/falling/matrix)
  - `BeatReaction`: None, SpeedBurst, Flash, SpawnMore, Pulse, ColorPop
  - `SpeedMultiplier`: double
  - `ColorIndex`: palette index
  - `PaletteId`: optional string — id of the palette for this layer (e.g. `"default"`). When null/empty, inherits from `TextLayers.PaletteId`.
  - `ImageFolderPath`: optional string (path to folder with images; for AsciiImage only)
  - `AsciiImageMovement`: None, Scroll, Zoom, Both (for AsciiImage; default Scroll)
  - `Gain`: double (1.0–10.0, default 2.5; for Oscilloscope layer only)

## Key bindings

- **P** — Cycle the color palette of the **active layer** (the layer last selected with 1–9). Saved to that layer's settings.
- **S** — Open settings modal (two-column: layer list on left, selected layer settings on right; 1–9 select, ↑/↓ select, ←/→ change type, Shift+1–9 toggle enabled, ESC close). The modal replaces the header/toolbar region while keeping the visualizer visible below.
- **1–9** — Select the corresponding layer as active (no type change). Key 1 = layer 1 (back), key 9 = layer 9 (front). Number keys and numpad keys work.
- **←/→** (Left/Right arrow) — Cycle the active layer's type forward or backward (includes None). Changes persist to appsettings.json.
- **Shift+1–9** — Toggle the corresponding layer enabled/disabled. Disabled layers are not rendered. Changes persist to appsettings.json.
- **I** — Cycle to the next picture in AsciiImage layers (only when at least one layer is AsciiImage).
- **[ / ]** — Adjust gain (1.0–10.0) when the selected layer is Oscilloscope.
- Toolbar suffix: shows layers as "123456789" (active in highlight, disabled dimmed); key hints (1–9 select, ←→ type, Shift+1–9 toggle); "Gain: X.X ([ ])" when Oscilloscope layer is selected; "Palette (LN): name (P)" for the active layer; "I: next image" when AsciiImage exists

## Viewport constraints

- Minimum width: 10
- Minimum height: 3 lines
- Uses full `viewport.Width` × `viewport.MaxLines` for cell buffer
- If no layers configured, renders empty gray buffer

## Implementation notes

- **Internal state**: `ViewportCellBuffer`; `_layerStates` (offset, snippet index per layer); `_fallingLettersByLayer` (particles for FallingLetters); `_asciiImageStateByLayer` (scroll, zoom, cache for AsciiImage); `_geissBackgroundStateByLayer` (phase, colorPhase, bass/treble for GeissBackground); `_beatCirclesStateByLayer` (circles, lastBeatCount for BeatCircles); `_paletteCycleLayerIndex` (layer whose palette P cycles); `_lastBeatCount`; `_beatFlashFrames`.
- **Cell buffer**: Per [ADR-0005](../adr/0005-layered-visualizer-cell-buffer.md); internal to this visualizer only.
- **None layer**: `LayerType.None` renders nothing; no renderer registered. Use ←/→ to cycle type (includes None). Use Shift+1–9 to toggle layer enabled/disabled.
- **Layer types**: Each type has its own class implementing `ITextLayerRenderer`, in a per-layer subfolder: `ScrollingColors/ScrollingColorsLayer`, `Marquee/MarqueeLayer`, `FallingLetters/FallingLettersLayer`, `MatrixRain/MatrixRainLayer`, `WaveText/WaveTextLayer`, `StaticText/StaticTextLayer`, `AsciiImage/AsciiImageLayer`, `GeissBackground/GeissBackgroundLayer`, `BeatCircles/BeatCirclesLayer`, `Oscilloscope/OscilloscopeLayer`. Shared infrastructure (ITextLayerRenderer, TextLayerSettings, TextLayerDrawContext, etc.) stays in the TextLayers root.
- **GeissBackground layer**: Psychedelic plasma-style background; sine-based plasma with bass/treble modulation; uses SmoothedMagnitudes and TargetMaxMagnitude; Flash beat reaction boosts plasma intensity; palette or GetGeissColor fallback.
- **BeatCircles layer**: Expanding circles spawned on beat; draws only circle pixels (transparent elsewhere); uses BeatCount, SmoothedMagnitudes for maxRadius; up to 5 circles; aspect ratio 2.0 for elliptical appearance.
- **Oscilloscope layer**: Time-domain waveform; uses Waveform, WaveformPosition, WaveformSize; per-layer Gain (1.0–10.0); [ ] adjusts gain when layer is selected; color by distance from center (cyan/green/yellow/red).
- **AsciiImage layer**: Reads images (BMP, GIF, JPEG, PNG, WebP) from `ImageFolderPath`, converts to ASCII via grayscale-to-character mapping; supports scroll, zoom, or both via `AsciiImageMovement`; uses palette gradient for colored output; Flash beat reaction cycles to next image. Depends on SixLabors.ImageSharp.
- **Beat reactions**: SpeedBurst (faster), Flash (advance/change), SpawnMore (spawn particles), Pulse (amplitude/color change), ColorPop (color offset).
- **References**: [ADR-0004](../adr/0004-visualizer-encapsulation.md), [ADR-0005](../adr/0005-layered-visualizer-cell-buffer.md).
