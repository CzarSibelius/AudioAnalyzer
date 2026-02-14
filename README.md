# Audio Analyzer

A real-time audio analyzer that captures and analyzes system audio output using NAudio. This application captures the audio playing on your system (loopback capture), from a selected capture device, or uses **Demo Mode** (synthetic BPM stream for testing without audio). It performs FFT analysis with multiple visualization modes: spectrum bars, VU meter, Winamp-style bars, and **Layered text** (configurable text snippets and layer types—Geiss plasma background, beat circles, oscilloscope, Unknown Pleasures stacked waveforms, marquee, falling letters, ASCII images, etc.—with beat-reactive behavior).

## Prerequisites

- .NET 10.0 SDK or later
- Windows operating system (uses WASAPI loopback/capture)
- NAudio package (automatically restored when building)

## How to Run

### Using Visual Studio

1. Open `AudioAnalyzer.sln` in Visual Studio
2. Set **AudioAnalyzer.Console** as the startup project (or press F5 to build and run)

### Using Command Line

From the solution root (the folder containing `AudioAnalyzer.sln`):

1. Restore and build:
   ```bash
   dotnet build
   ```
   The solution uses static analysis: code style from `.editorconfig` and Roslynator rules (e.g. RCS1075: no empty catch blocks, RCS1060: one file per class) are enforced at build time (`Directory.Build.props`). To verify formatting without changing files, run `dotnet format .\AudioAnalyzer.sln --verify-no-changes` (or `dotnet format .\AudioAnalyzer.sln` to fix).

2. Run the application:
   ```bash
   dotnet run --project src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj
   ```

On Windows you can use backslashes: `src\AudioAnalyzer.Console\AudioAnalyzer.Console.csproj`.

## Usage

1. On first run (or if no device was saved), choose an audio input: **Demo Mode** (synthetic stream at 90/120/140 BPM for testing without audio), loopback (system output), or a specific capture device (↑/↓ to move, ENTER to select, ESC to cancel).
2. The analyzer shows real-time volume and frequency analysis. Play audio to see it in action.
3. **Keyboard controls:**
   - **H** – Show help (all keys and visualization modes)
   - **V** – Cycle visualization mode (Spectrum, VU Meter, Winamp Style, Layered text)
   - **P** – Cycle color palette (for palette-aware visualizers; affects only the current visualizer and persists to that visualizer's settings)
   - **+** / **-** – Increase / decrease beat sensitivity
   - **[** / **]** – Increase / decrease oscilloscope gain (Layered text when Oscilloscope layer is selected; 1.0–10.0)
   - **1–9** – Select layer as active (Layered text mode; 1 = layer 1 (back), 9 = layer 9 (front))
   - **←/→** – Cycle active layer's type (Layered text mode)
   - **Shift+1–9** – Toggle layer enabled/disabled (Layered text mode)
   - **I** – Cycle to next picture (Layered text mode, when an AsciiImage layer is active)
   - **S** – Open TextLayers settings modal (Layered text mode only; 1–9 select, ←→ type, Shift+1–9 toggle, ESC close)
   - **D** – Change audio input device
   - **F** – Toggle full screen (visualizer only, no header/toolbar)
   - **ESC** – Quit

   Settings (device, mode, per-visualizer palette, beat sensitivity, oscilloscope gain) are saved automatically when you change them.

## What It Does

- **Audio input**: Demo Mode (synthetic BPM stream for testing), loopback (system output), or a specific WASAPI capture device; choice is saved in settings.
- **Volume analysis**: Real-time level and peak display; stereo VU-style meters in VU Meter mode.
- **FFT analysis**: Fast Fourier Transform with log-spaced frequency bands and peak hold.
- **Visualization modes**: Spectrum bars, VU Meter, Winamp-style bars, and **Layered text** (multiple independent layers—e.g. Geiss plasma background, beat circles, oscilloscope, Unknown Pleasures stacked waveforms, scrolling colors, marquee, falling letters, ASCII images from a folder—with configurable text snippets and beat reactions; each layer has its own palette; press **1–9** to select a layer, **←/→** to change its type, **Shift+1–9** to toggle enabled; **[ / ]** to adjust oscilloscope gain when that layer is selected; press **P** to cycle the active layer's palette).
- **Colors and palettes**: Palette-aware visualizers (Layered text layers) support **24-bit true color** (RGB) and 16 console colors. Palettes are stored as JSON files in a **palettes** directory (see below). Each layer has its own palette setting; pressing **P** affects only the active layer and saves to that layer's settings.
- **Beat detection**: Optional beat detection and BPM estimate; sensitivity and beat circles are configurable and persist.
- **Real-time display**: Updates every 50 ms.
- **Toolbar**: Shows volume, mode, layer hints, palette, and H=Help. When the help line exceeds the terminal width, it scrolls slowly back and forth instead of truncating.
- **Settings**: Stored in a local file (e.g. next to the executable). Per-visualizer options live under `VisualizerSettings`; each palette-aware visualizer has its own `PaletteId`. Device, visualization mode, per-visualizer palette, beat sensitivity, and oscilloscope gain are saved automatically when changed.

## Dependencies

- **NAudio 2.2.1**: WASAPI capture/loopback and audio processing
- **SixLabors.ImageSharp 3.1.12**: Image loading and processing for ASCII image layer (BMP, GIF, JPEG, PNG, WebP)
- **Microsoft.Extensions.DependencyInjection 10.0.3**: Used by the Console host (dependency injection)
- **Roslynator.Analyzers 4.15.0**: Code analyzers (e.g. RCS1075: no empty catch blocks, RCS1060: one file per class), enforced via `.editorconfig`

## Palettes (JSON files)

Color palettes are stored as **JSON files** in a **`palettes`** directory next to the executable (e.g. `palettes/` in the same folder as the .exe). The app ships with `palettes/default.json` and `palettes/oscilloscope.json` (classic waveform gradient: Cyan → Green → Yellow → Red). You can add more `.json` files; each file is one palette. Press **P** to cycle through all available palettes when using a palette-aware visualizer; the change applies only to the current visualizer and is saved to that visualizer's settings.

**Palette JSON format:**

- **`Name`** (optional): Display name (e.g. `"Power Corruption & Lies"`).
- **`Colors`**: Array of color entries. Each entry can be:
  - A **string**: hex `"#RRGGBB"` (24-bit) or a console color name (e.g. `"Magenta"`).
  - An **object**: `{ "R": 255, "G": 0, "B": 0 }` (values 0–255).

Example (`palettes/default.json`):

```json
{
  "Name": "Default",
  "Colors": ["Magenta", "Yellow", "Green", "Cyan", "Blue"]
}
```

Example with 24-bit colors:

```json
{
  "Name": "Sunset",
  "Colors": ["#FF6B35", "#F7C59F", "#2A9D8F", "#264653", "#E9C46A"]
}
```

## Settings structure (per-visualizer)

- **Visualizer-specific options** live under `VisualizerSettings` in the settings file (e.g. `appsettings.json`):
  - **Layered text**: `VisualizerSettings.TextLayers` — `PaletteId` fallback for layers; list of 9 layers (keys 1–9) with `LayerType`, `Enabled`, `ZOrder`, `TextSnippets`, `BeatReaction`, `SpeedMultiplier`, `ColorIndex`, `PaletteId` (per-layer palette; inherits from TextLayers.PaletteId when empty), and for AsciiImage: `ImageFolderPath`, `AsciiImageMovement`; for Oscilloscope: `Gain` (1.0–10.0). Layer types: `None`, `ScrollingColors`, `Marquee`, `FallingLetters`, `MatrixRain`, `WaveText`, `StaticText`, `AsciiImage`, `GeissBackground`, `BeatCircles`, `Oscilloscope`, `UnknownPleasures`. Beat reactions: `None`, `SpeedBurst`, `Flash`, `SpawnMore`, `Pulse`, `ColorPop`. Layers are drawn in ascending `ZOrder` (lower = back). Press 1–9 to select a layer, ←/→ to change its type, Shift+1–9 to toggle enabled; [ ] adjust Oscilloscope layer gain; P cycles the active layer's palette; changes persist to appsettings.json.

Example JSON:

```json
"VisualizerSettings": {
  "TextLayers": {
    "PaletteId": "default",
    "Layers": [
      { "LayerType": "ScrollingColors", "ZOrder": 0, "BeatReaction": "ColorPop", "SpeedMultiplier": 1.0 },
      { "LayerType": "Marquee", "ZOrder": 1, "TextSnippets": ["Layered text", "Audio visualizer"], "BeatReaction": "SpeedBurst", "SpeedMultiplier": 1.0 },
      { "LayerType": "AsciiImage", "ZOrder": 2, "ImageFolderPath": "C:\\Pictures", "AsciiImageMovement": "Both", "BeatReaction": "SpeedBurst", "SpeedMultiplier": 1.0 }
    ]
  }
}
```

Legacy top-level `BeatCircles` (migrated into TextLayers BeatCircles layer enabled state) and `OscilloscopeGain` are still read for backward compatibility and merged into `VisualizerSettings` when loading if the new structure is missing.

## Notes

- Requires Windows with WASAPI support.
- Use loopback to analyze system playback; use a capture device for microphone or other inputs.
- Ensure audio is playing (or that the selected device is active) to see meaningful analysis. Use **Demo Mode** when you want to test visualizers without playing music.
- **Modal dialogs** (help screen, device selection, and any future dialogs) use a shared modal system ([ADR-0006](docs/adr/0006-modal-system.md)): they are drawn on top of the main view, capture input until you dismiss them (e.g. any key for help, ENTER/ESC for device menu), then the main view is redrawn automatically.

## Visualizer bounds (for developers)

Visualizers implement **`IVisualizer`** and expose **technical name** (stable key for settings/CLI, e.g. `"geiss"`), **display name** (for toolbar and help), and **`SupportsPaletteCycling`** (whether the visualizer uses a per-visualizer palette when the user presses P). Optional **`GetToolbarSuffix(snapshot)`** can return mode-specific toolbar text (e.g. gain for waveform modes). Visualizers that need configuration receive their settings via constructor injection (see [ADR-0008](docs/adr/0008-visualizer-settings-di.md)). The composite renderer uses only this interface and the shared snapshot; it does not reference concrete visualizer types (see [ADR-0004](docs/adr/0004-visualizer-encapsulation.md)). **New visualizer content** should be created as `ITextLayerRenderer` layers in TextLayersVisualizer, not as standalone IVisualizer modes — see [ADR-0014](docs/adr/0014-visualizers-as-layers.md).

Visualizers receive a **viewport** (`VisualizerViewport`: start row, max lines, width). They must not write more than `viewport.MaxLines` lines and no line longer than `viewport.Width`. The composite renderer validates dimensions and display start row before calling visualizers; if a visualizer throws, the exception message is shown in the viewport (one line, truncated to width) and the next frame can recover — see [ADR-0012](docs/adr/0012-visualizer-exception-handling.md). This keeps resizes and bad data from corrupting the console UI.

Per-visualizer specs (behavior, settings, viewport constraints) are in [docs/visualizers/](docs/visualizers/README.md). C# coding standards (including no empty try-catch, non-empty XML summaries, one file per class) are in `.cursor/rules/csharp-standards.mdc`, `.cursor/rules/no-empty-catch.mdc`, and [ADR-0016](docs/adr/0016-csharp-documentation-and-file-organization.md).
