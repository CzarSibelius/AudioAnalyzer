# Audio Analyzer

A real-time audio analyzer that captures and analyzes system audio output using NAudio. This application captures the audio playing on your system (loopback capture) or from a selected capture device and performs FFT analysis with multiple visualization modes: spectrum bars, oscilloscope, VU meter, Winamp-style bars, Geiss-style visualization, Unknown Pleasures (stacked waveform snapshots with a configurable palette), and **Layered text** (configurable text snippets and layer types with beat-reactive behavior).

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
   The solution uses static analysis: code style from `.editorconfig` is enforced at build time (`Directory.Build.props`). To verify formatting without changing files, run `dotnet format .\AudioAnalyzer.sln --verify-no-changes` (or `dotnet format .\AudioAnalyzer.sln` to fix).

2. Run the application:
   ```bash
   dotnet run --project src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj
   ```

On Windows you can use backslashes: `src\AudioAnalyzer.Console\AudioAnalyzer.Console.csproj`.

## Usage

1. On first run (or if no device was saved), choose an audio input: loopback (system output) or a specific capture device (↑/↓ to move, ENTER to select, ESC to cancel).
2. The analyzer shows real-time volume and frequency analysis. Play audio to see it in action.
3. **Keyboard controls:**
   - **H** – Show help (all keys and visualization modes)
   - **V** – Cycle visualization mode (Spectrum, Oscilloscope, VU Meter, Winamp Style, Geiss, Unknown Pleasures, Layered text)
   - **P** – Cycle color palette (for palette-aware visualizers: Geiss, Unknown Pleasures, Layered text)
   - **B** – Toggle beat circles (Geiss mode)
   - **+** / **-** – Increase / decrease beat sensitivity
   - **[** / **]** – Increase / decrease oscilloscope gain (Oscilloscope mode; 1.0–10.0)
   - **D** – Change audio input device
   - **F** – Toggle full screen (visualizer only, no header/toolbar)
   - **ESC** – Quit

   Settings (device, mode, selected palette, beat sensitivity, oscilloscope gain, beat circles) are saved automatically when you change them.

## What It Does

- **Audio input**: Loopback (system output) or a specific WASAPI capture device; choice is saved in settings.
- **Volume analysis**: Real-time level and peak display; stereo VU-style meters in VU Meter mode.
- **FFT analysis**: Fast Fourier Transform with log-spaced frequency bands and peak hold.
- **Visualization modes**: Spectrum bars, Oscilloscope (time-domain waveform; gain adjustable with [ ] in real time, 1.0–10.0), VU Meter, Winamp-style bars, **Geiss** (psychedelic plasma; optional beat circles; uses the selected color palette; press **P** to cycle palettes), **Unknown Pleasures** (stacked waveform snapshots; uses the selected color palette; press **P** to cycle palettes), and **Layered text** (multiple independent layers—e.g. scrolling colors, marquee, falling letters—with configurable text snippets and beat reactions; config in settings; press **P** to cycle palettes).
- **Colors and palettes**: Palette-aware visualizers (Geiss, Unknown Pleasures, Layered text) support **24-bit true color** (RGB) and 16 console colors. Palettes are stored as JSON files in a **palettes** directory (see below). The selected palette is applied when using those modes.
- **Beat detection**: Optional beat detection and BPM estimate; sensitivity and beat circles are configurable and persist.
- **Real-time display**: Updates every 50 ms.
- **Settings**: Stored in a local file (e.g. next to the executable). `SelectedPaletteId` stores the current palette; per-visualizer options live under `VisualizerSettings`. Device, visualization mode, selected palette, beat sensitivity, oscilloscope gain, and beat circles are saved automatically when changed.

## Dependencies

- **NAudio 2.2.1**: WASAPI capture/loopback and audio processing
- **Microsoft.Extensions.DependencyInjection 9.0.0**: Used by the Console host (dependency injection)

## Palettes (JSON files)

Color palettes are stored as **JSON files** in a **`palettes`** directory next to the executable (e.g. `palettes/` in the same folder as the .exe). The app ships with `palettes/default.json`. You can add more `.json` files; each file is one palette. Press **P** to cycle through all available palettes when using a palette-aware visualizer (Geiss or Unknown Pleasures).

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

- **Selected palette**: `SelectedPaletteId` — id of the current palette (e.g. filename without extension, like `"default"`). Resolved from the palettes directory on startup.
- **Visualizer-specific options** live under `VisualizerSettings` in the settings file (e.g. `appsettings.json`):
  - **Unknown Pleasures**: Legacy `VisualizerSettings.UnknownPleasures.Palette` (ColorPalette with `ColorNames`) is still read if `SelectedPaletteId` is not set, for backward compatibility.
  - **Geiss**: `VisualizerSettings.Geiss.BeatCircles` — show beat circles (boolean).
  - **Oscilloscope**: `VisualizerSettings.Oscilloscope.Gain` — amplitude gain (1.0–10.0).
  - **Layered text**: `VisualizerSettings.TextLayers` — list of layers, each with `LayerType`, `ZOrder`, `TextSnippets`, `BeatReaction`, `SpeedMultiplier`, `ColorIndex`. Layer types: `ScrollingColors`, `Marquee`, `FallingLetters`, `MatrixRain`, `WaveText`, `StaticText`. Beat reactions: `None`, `SpeedBurst`, `Flash`, `SpawnMore`, `Pulse`, `ColorPop`. Layers are drawn in ascending `ZOrder` (lower = back). Edit `appsettings.json` to add, remove, or reconfigure layers.

Example JSON:

```json
"SelectedPaletteId": "default",
"VisualizerSettings": {
  "Geiss": { "BeatCircles": true },
  "Oscilloscope": { "Gain": 2.5 },
  "TextLayers": {
    "Layers": [
      { "LayerType": "ScrollingColors", "ZOrder": 0, "BeatReaction": "ColorPop", "SpeedMultiplier": 1.0 },
      { "LayerType": "Marquee", "ZOrder": 1, "TextSnippets": ["Layered text", "Audio visualizer"], "BeatReaction": "SpeedBurst", "SpeedMultiplier": 1.0 }
    ]
  }
}
```

Legacy top-level `BeatCircles` and `OscilloscopeGain` are still read for backward compatibility and merged into `VisualizerSettings` when loading if the new structure is missing.

## Notes

- Requires Windows with WASAPI support.
- Use loopback to analyze system playback; use a capture device for microphone or other inputs.
- Ensure audio is playing (or that the selected device is active) to see meaningful analysis.
- **Modal dialogs** (help screen, device selection, and any future dialogs) use a shared modal system ([ADR-0006](docs/adr/0006-modal-system.md)): they are drawn on top of the main view, capture input until you dismiss them (e.g. any key for help, ENTER/ESC for device menu), then the main view is redrawn automatically.

## Visualizer bounds (for developers)

Visualizers implement **`IVisualizer`** and expose **technical name** (stable key for settings/CLI, e.g. `"geiss"`), **display name** (for toolbar and help), and **`SupportsPaletteCycling`** (whether the visualizer uses the global palette when the user presses P). Optional **`GetToolbarSuffix(snapshot)`** can return mode-specific toolbar text (e.g. gain for waveform modes). The composite renderer uses only this interface and the shared snapshot; it does not reference concrete visualizer types (see [ADR-0004](docs/adr/0004-visualizer-encapsulation.md)).

Visualizers receive a **viewport** (`VisualizerViewport`: start row, max lines, width). They must not write more than `viewport.MaxLines` lines and no line longer than `viewport.Width`. The composite renderer validates dimensions and display start row before calling visualizers; if a visualizer throws, a one-line error is shown and the next frame can recover. This keeps resizes and bad data from corrupting the console UI.
