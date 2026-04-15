# Configuration reference

This document describes data files and settings beside the Audio Analyzer executable: dependency versions (for support and reproducibility), JSON formats for presets, shows, and palettes, sample assets, and `appsettings.json`. For a short project intro and how to run the app, see the root [README.md](../README.md).

## Dependencies (NuGet)

- **NAudio 2.2.1**: WASAPI capture/loopback and audio processing
- **SixLabors.ImageSharp 3.1.12**: Image loading and processing for ASCII image layer (BMP, GIF, JPEG, PNG, WebP) and in-memory BGRA raster → ASCII for the **AsciiVideo** text layer
- **System.IO.Abstractions 22.1.0**: `IFileSystem` for presets/palettes/shows (Infrastructure) and AsciiImage / AsciiModel asset enumeration and file reads (Visualizers); production uses the real `FileSystem`, tests use `MockFileSystem`
- **Microsoft.Extensions.DependencyInjection 10.0.3**: Console host dependency injection
- **Microsoft.Extensions.Logging 10.0.3** / **Microsoft.Extensions.Logging.Abstractions 10.0.3**: optional file logging (ADR-0076); Abstractions referenced from Infrastructure for the file logger provider; **Platform.Windows** references both for `WindowsAsciiVideoFrameSource` `[LoggerMessage]` diagnostics
- **Roslynator.Analyzers 4.15.0**: Code analyzers (e.g. RCS1075: no empty catch blocks, RCS1060: one file per class), enforced via `.editorconfig`

**Test project only (not shipped with the app):**

- **Microsoft.NET.Test.Sdk 17.14.0**: VSTest host; built-in **TRX** logger for per-test durations (`--logger "trx;LogFileName=…"`).
- **JunitXml.TestLogger 8.0.0**: JUnit XML report for CI and parsers (`--logger "junit;LogFilePath=…"`). See [docs/agents/testing-and-verification.md](agents/testing-and-verification.md#slow-test-reports-trx-and-junit).

## Presets (JSON files)

TextLayers presets are stored as **JSON files** in a **`presets`** directory next to the executable (e.g. `presets/` in the same folder as the .exe). Each file is one preset. The app ships with **`presets/fill-blendover-demo.json`**, a small example of Fill **BlendOver** over an oscilloscope (see [visualizers/fill.md](visualizers/fill.md) Troubleshooting). Press **V** to cycle presets; **S** to edit (R rename, N new preset). Presets are created automatically on first run.

**Preset JSON format:**

- **`Name`** (optional): Display name (e.g. `"Preset 1"`).
- **`Config`**: TextLayersVisualizerSettings — `PaletteId` plus `Layers` array (0 up to `TextLayersLimits.MaxLayerCount` entries, each with `LayerType`, `Enabled`, `ZOrder`, `SpeedMultiplier`, `ColorIndex`, `PaletteId`, optional `RenderBounds`, and `Custom`). Phrase lists live in **`Custom.TextSnippets`** only for layer types that use whole phrases (Marquee, WaveText, StaticText, NowPlaying, Maschine). **FallingLetters** uses **`CharsetId`** (and optional `AnimationMode`) for its glyph pool, not `TextSnippets` ([ADR-0081](adr/0081-consolidate-matrix-rain-into-falling-letters.md)). A **root-level** `"TextSnippets"` property on a layer object is **not** read (ignored by the serializer); move values into `Custom` per [ADR-0029](adr/0029-no-settings-migration.md). Layers that support beat reaction store it in `Custom` (e.g. `Custom.BeatReaction`); not all layer types expose beat reaction. Inside each layer’s **`Custom`** object, enum fields accept **either** JSON strings (e.g. `"ImageColors"`) **or** numeric values, and property names are matched **case-insensitively** (so `paletteSource` and `PaletteSource` both work). The app uses the same rules when saving from the S modal.

Example (`presets/preset-1.json`):

```json
{
  "Name": "Preset 1",
  "Config": { "PaletteId": "default", "Layers": [...] }
}
```

**AsciiVideo layer `Custom` example** (inside one object in `Layers` with `"LayerType": "AsciiVideo"`):

```json
"Custom": {
  "SourceKind": "Webcam",
  "WebcamDeviceIndex": 0,
  "MaxCaptureWidth": 640,
  "MaxCaptureHeight": 480,
  "PaletteSource": "ImageColors",
  "FlipHorizontal": false
}
```

For **Palette source**, use `"LayerPalette"` or `"ImageColors"` (or `0` / `1`). Use `0` for **MaxCaptureWidth** / **MaxCaptureHeight** when no resolution cap is desired. **`File`** is reserved for a future source; it is not implemented yet. Details: [visualizers/ascii-video.md](visualizers/ascii-video.md).

**BufferDistortion layer `Custom` example** (inside one object in `Layers` with `"LayerType": "BufferDistortion"`; place the layer **above** content to warp):

```json
"Custom": {
  "Mode": "PlaneWaves",
  "PlaneOrientation": "WaveAlongX",
  "PlaneAmplitudeCells": 2,
  "PlaneWavelengthCells": 14,
  "PlanePhaseSpeed": 1.2,
  "SpawnOnBeat": true,
  "MaxRipples": 8,
  "RippleWaveNumber": 0.9,
  "RippleTimeSpeed": 4.0,
  "RippleAmplitudeCells": 2,
  "RippleDecayPerSecond": 0.45,
  "RippleMaxAgeSeconds": 4.0,
  "MaxDisplacementCells": 6
}
```

Use `"Mode": "DropRipples"` for beat-spawned radial ripples (still set plane fields if you switch modes in the S modal). Details: [visualizers/buffer-distortion.md](visualizers/buffer-distortion.md).

**Waveform strip layer `Custom`** (`"LayerType": "WaveformStrip"`; see [visualizers/waveform-strip.md](visualizers/waveform-strip.md), [ADR-0079](adr/0079-waveform-strip-fixed-visible-seconds.md)):

- **`FixedVisibleSeconds`**: number (default **15**; clamped **1–120** when loaded) — trailing wall seconds mapped across the strip. Preset JSON may still contain a removed **`OverviewTimeMode`** key; it is ignored on deserialize.

```json
"Custom": {
  "FixedVisibleSeconds": 12
}
```

## Shows (JSON files)

Shows are stored as **JSON files** in a **`shows`** directory next to the executable. A Show is an ordered collection of presets with per-entry duration. Press **Tab** to switch to Show play; **S** in Show play to edit (add/remove entries, set duration). Duration can be in **Seconds** (wall-clock) or **Beats** (at the current BPM from the active **BPM source** — audio, demo, or Link).

**Show JSON format:**

- **`Name`** (optional): Display name (e.g. `"Live Set A"`).
- **`Entries`**: Array of `{ PresetId, Duration: { Unit: "Seconds"|"Beats", Value } }`.

Example (`shows/show-1.json`):

```json
{
  "Name": "Live Set A",
  "Entries": [
    { "PresetId": "preset-1", "Duration": { "Unit": "Seconds", "Value": 30 } },
    { "PresetId": "preset-2", "Duration": { "Unit": "Beats", "Value": 16 } }
  ]
}
```

## Palettes (JSON files)

Color palettes are stored as **JSON files** in a **`palettes`** directory next to the executable (e.g. `palettes/` in the same folder as the .exe). The app ships with `palettes/default.json`, `palettes/oscilloscope.json` (classic waveform gradient: Cyan → Green → Yellow → Red), `palettes/clear-black.json` (black-first palette useful when the buffer clear color should read as black for Fill **BlendOver**), themed sets such as `pine-forest`, `jungle`, `miami-1984`, and `radioactive`, UI-oriented themes `bill` (Windows 95–inspired), `ddos` (DOS/CGA-inspired), and `blue-pill` (Matrix-inspired), **four-color CGA hardware–style sets** (`cga-mode4-palette0-low` / `-high`, `cga-mode4-palette1-low` / `-high`, `cga-mode5-low` / `-high`; usable as layer palettes), **`c64`** (16-color Commodore 64 VIC-II order, Pepto-style RGB), and others. You can add more `.json` files; each file is one palette. Press **P** to cycle through all available palettes when using a palette-aware visualizer; the change applies only to the current visualizer and is saved to that visualizer's settings.

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

## Charsets (JSON files)

Reusable **ordered character sequences** for text layers (luminance ramps, density glyphs, random glyph pools) live in **`charsets/*.json`** next to the executable (see [ADR-0080](adr/0080-shared-charset-json-and-layer-charset-ids.md)). Id = filename without extension. The S modal **Charset** row opens a list (like palettes). **FallingLetters** defaults to the **`digits`** charset when `CharsetId` is unset ([ADR-0081](adr/0081-consolidate-matrix-rain-into-falling-letters.md)).

**Charsets vs `Custom.TextSnippets` (normative):**

- **Charsets** — when the layer **picks one character at a time** for rendering (ramps, density, random glyphs). Use `CharsetId` in the layer’s Custom settings and/or shared `charsets/*.json` files.
- **`Custom.TextSnippets`** — when the layer needs a **whole phrase** (or several phrases): Marquee, WaveText, StaticText, NowPlaying (fallback), Maschine.

**Charset JSON format:**

- **`Name`** (optional): display name in lists.
- **`Characters`** (required): a single JSON string, the ordered sequence of characters (including spaces when meaningful). Empty values are invalid; length is capped when loading/saving.

The app ships **`ascii-ramp-classic`**, **`density-soft`**, **`unknown-pleasures-ramp`**, **`english-alphabet`**, **`finnish-alphabet`**, **`digits`**, **`hex`**, and **`hex-lowercase`**. Add more `.json` files in `charsets/` to extend the list.

## UI themes (JSON files)

UI chrome colors are configured with **theme files** in a **`themes`** directory next to the executable (see [ADR-0071](adr/0071-ui-themes-separate-from-palettes.md)). General Settings **UI theme (T)** lists **`(Custom)`** and these files only (not every layer palette). **N** in the theme modal starts **new theme from palette**: pick a source palette, map 11 semantic slots to palette indices, then save (creates `theme-1.json`, `theme-2.json`, …). The app ships **`themes/default.json`** and **`themes/c64.json`** (Commodore 64 chrome with `FallbackPaletteId` **`c64`** for hub swatches).

**Theme JSON format:**

- **`Name`** (optional): display name in lists.
- **`FallbackPaletteId`** (optional): layer palette id used for (a) base UI/title-bar mapping via the same index rules as [ADR-0065](adr/0065-ui-theme-from-layer-palettes.md) / `UiThemePaletteMapper`, and (b) animated swatches in the theme list and General Settings hub when set.
- **`Ui`** (optional object): semantic slots `Normal`, `Highlighted`, `Dimmed`, `Label`, optional `Background` — each a color entry like palette colors.
- **`TitleBar`** (optional object): `AppName`, `Mode`, `Preset`, `Layer`, `Separator`, `Frame` — same entry shape.

Explicit slots **override** the fallback mapping; omitted slots keep the base.

Example (`themes/default.json` ships with the app):

```json
{
  "Name": "Default theme",
  "FallbackPaletteId": "default"
}
```

`appsettings.json` holds **`UiSettings.UiThemeId`** (filename without extension, or null/`(Custom)` for inline `Palette` / `TitleBarPalette`).

## Sample OBJ models (AsciiModel)

The **AsciiModel** text layer loads **Wavefront OBJ** files from a folder. The app ships **`models/sample/`** next to the executable (`cube.obj`, `tetrahedron.obj`; see `models/sample/README.md`). In the S modal, set **Model folder** to that directory (e.g. `models\sample` when running from the build output folder). Press **I** to cycle `.obj` files in alphabetical order. **Ambient**, **Lighting preset** (Classic, Headlight, or Custom angles), and related options reduce overly dark sides; details are in [visualizers/ascii-model.md](visualizers/ascii-model.md).

## Settings structure (`appsettings.json`)

- **BPM source** (`BpmSource`, per [ADR-0066](adr/0066-bpm-source-and-ableton-link.md)): Where tempo and the beat counter come from. Values: `AudioAnalysis` (default, energy detection on the audio stream), `DemoDevice` (BPM from the active `demo:NNN` synthetic device + time-based beats), `AbletonLink` (Ableton Link session via native `link_shim.dll`). Editable from General settings hub (**BPM source** row, **Enter** to cycle). FFT/spectrum/waveform always follow the **audio input**, not Link.

- **Max audio history** (`MaxAudioHistorySeconds`, per [ADR-0077](adr/0077-waveform-overview-snapshot.md)): How many seconds of **mono** waveform the engine retains for the decimated **waveform strip** overview. Clamped to **5–180** when loaded or saved. Default **60**. The internal ring size is `seconds × sample rate` capped at **5,000,000** samples (~20 MB float buffer at the cap). Changing the value in General settings (**Max audio history** row: **+**/**−**, **Enter** to type seconds) resizes the ring and persists settings. The oscilloscope still uses a **512-sample** scope window from the same stream.

- **Logging** (`Logging`, per [ADR-0076](adr/0076-configurable-application-logging.md)): Optional file logging for diagnostics. **`Enabled`** (`bool`, default `false`): when `true`, log lines at or above **`MinimumLevel`** are appended to **`FilePath`**. **`FilePath`** (optional string): relative paths are under the application base directory (next to the executable); when omitted or empty, defaults to `logs/audioanalyzer.log`. **`MinimumLevel`** (string): `Trace`, `Debug`, `Information`, `Warning`, `Error`, or `Critical` (same names as `Microsoft.Extensions.Logging.LogLevel`); invalid values are treated as `Error`. Default when omitted: **`Error`**. Log files may contain paths, device names, or other environment-specific text; enable only when you accept that privacy trade-off. Restart the app after editing this section (or reload settings the same way as other `appsettings.json` changes). **ASCII video (webcam)** failures and empty-device cases are logged under category **`AudioAnalyzer.Platform.Windows.AsciiVideo.WindowsAsciiVideoFrameSource`** (use **`Warning`** or lower to see them; **`Debug`** includes resolution-cap attempts).

- **UI settings** (`UiSettings`, per [ADR-0033](adr/0033-ui-principles-and-configurable-settings.md), [ADR-0071](adr/0071-ui-themes-separate-from-palettes.md), [ADR-0067](adr/0067-60fps-target-and-render-fps-overlay.md), [ADR-0072](adr/0072-delta-time-display-animation.md), [ADR-0073](adr/0073-layer-render-time-overlay.md)): App title, optional `TitleBarAppName` (short name for title bar, e.g. "aUdioNLZR"), optional `DefaultAssetFolderPath` (default base directory for AsciiImage / AsciiModel when the layer’s folder setting is empty; omit to use the app content root), optional `UiThemeId` (`themes/*.json` id — when set and the file loads, effective UI/title bar colors come from the theme; omit or use General settings **(Custom)** for inline colors), optional `TitleBarPalette` (used for base when no theme or theme has no usable fallback), UI palette (Normal, Highlighted, Dimmed, Label, optional Background), **`DefaultScrollingSpeed`** (character advance per 60 Hz reference frame; scaled by display frame delta per ADR-0072), optional **`ShowRenderFps`** (`bool`, default `false`) to show smoothed main-render FPS on the toolbar (main-loop cadence—typically **≥~60** FPS on capable hosts, independent of WASAPI buffer rate), and optional **`ShowLayerRenderTime`** (`bool`, default `false`) to show each text layer’s last measured `Draw` time in the S modal (General settings hub). Stored in `appsettings.json`. Colors support 24-bit RGB (`R`, `G`, `B`) or console color names (`Value`).

- **Visualizer-specific options** live under `VisualizerSettings` in the settings file (e.g. `appsettings.json`):
  - **Presets**: Stored as individual JSON files in the `presets/` directory (see above). `ActivePresetId` references the active preset. Press **V** to cycle presets (Preset editor); **S** to edit (R rename, N new preset).
  - **Shows**: Stored in the `shows/` directory. `ActiveShowId` and `ApplicationMode` (PresetEditor or ShowPlay) determine current mode. **Tab** switches modes; **S** in Show play opens Show edit.

Example `appsettings.json`:

```json
{
  "BpmSource": "AudioAnalysis",
  "MaxAudioHistorySeconds": 60,
  "Logging": {
    "Enabled": false,
    "FilePath": "logs/audioanalyzer.log",
    "MinimumLevel": "Error"
  },
  "UiSettings": {
    "Title": "AUDIO ANALYZER - Real-time Frequency Spectrum",
    "DefaultScrollingSpeed": 0.25,
    "ShowRenderFps": false,
    "ShowLayerRenderTime": false,
    "UiThemeId": null,
    "Palette": {
      "Normal": { "Value": "Gray" },
      "Highlighted": { "Value": "Yellow" },
      "Dimmed": { "Value": "DarkGray" },
      "Label": { "Value": "DarkCyan" }
    }
  },
  "VisualizerSettings": {
    "ActivePresetId": "preset-1",
    "ApplicationMode": "PresetEditor",
    "ActiveShowId": null
  }
}
```

If the settings file is corrupt or incompatible (e.g. invalid JSON), the app backs it up to `appsettings.{timestamp}.bak` (UTC, e.g. `appsettings.2025-02-17T14-30-00.123.bak`) and creates new defaults. You can recover values from the `.bak` file manually if needed — see [ADR-0029](adr/0029-no-settings-migration.md).

## Ableton Link native build (`link_shim.dll`)

Optional. Clone [Ableton/link](https://github.com/Ableton/link) into **`native/third_party/link`** (or set `LINK_ROOT` when configuring CMake). From **`native/link-shim`**:

```bash
cmake -B build -A x64
cmake --build build --config Release
```

Copy **`link_shim.dll`** next to `AudioAnalyzer.Console.exe`. The app runs without it; **Ableton Link** BPM mode then shows a “no native DLL” hint until the DLL is present. Link peers must be on the **same LAN**; see [Ableton Link documentation](https://ableton.github.io/link/). **GPL-2.0+** applies to Link and the shim when you build or ship that DLL; the project as a whole is **GPL-3.0-only** — see root `LICENSE`, `NOTICE`, and [ADR-0066](adr/0066-bpm-source-and-ableton-link.md). Official releases from this repository do not ship `link_shim.dll`.

**Agents and CI:** toolchain prerequisites (CMake + MSVC Build Tools, Developer PowerShell for VS), verification, and a checklist are in [docs/agents/native-link-shim-build.md](agents/native-link-shim-build.md).
