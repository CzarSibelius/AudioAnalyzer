# Configuration reference

This document describes data files and settings beside the Audio Analyzer executable: dependency versions (for support and reproducibility), JSON formats for presets, shows, and palettes, sample assets, and `appsettings.json`. For a short project intro and how to run the app, see the root [README.md](../README.md).

## Dependencies (NuGet)

- **NAudio 2.2.1**: WASAPI capture/loopback and audio processing
- **SixLabors.ImageSharp 3.1.12**: Image loading and processing for ASCII image layer (BMP, GIF, JPEG, PNG, WebP)
- **Microsoft.Extensions.DependencyInjection 10.0.3**: Console host dependency injection
- **Roslynator.Analyzers 4.15.0**: Code analyzers (e.g. RCS1075: no empty catch blocks, RCS1060: one file per class), enforced via `.editorconfig`

## Presets (JSON files)

TextLayers presets are stored as **JSON files** in a **`presets`** directory next to the executable (e.g. `presets/` in the same folder as the .exe). Each file is one preset. The app ships with **`presets/fill-blendover-demo.json`**, a small example of Fill **BlendOver** over an oscilloscope (see [visualizers/fill.md](visualizers/fill.md) Troubleshooting). Press **V** to cycle presets; **S** to edit (R rename, N new preset). Presets are created automatically on first run.

**Preset JSON format:**

- **`Name`** (optional): Display name (e.g. `"Preset 1"`).
- **`Config`**: TextLayersVisualizerSettings — `PaletteId` plus `Layers` array (9 layers with `LayerType`, `Enabled`, `ZOrder`, `TextSnippets`, `SpeedMultiplier`, etc.). Layers that support beat reaction store it in `Custom` (e.g. `Custom.BeatReaction`); not all layer types expose beat reaction.

Example (`presets/preset-1.json`):

```json
{
  "Name": "Preset 1",
  "Config": { "PaletteId": "default", "Layers": [...] }
}
```

## Shows (JSON files)

Shows are stored as **JSON files** in a **`shows`** directory next to the executable. A Show is an ordered collection of presets with per-entry duration. Press **Tab** to switch to Show play; **S** in Show play to edit (add/remove entries, set duration). Duration can be in **Seconds** (wall-clock) or **Beats** (at current detected BPM).

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

Color palettes are stored as **JSON files** in a **`palettes`** directory next to the executable (e.g. `palettes/` in the same folder as the .exe). The app ships with `palettes/default.json`, `palettes/oscilloscope.json` (classic waveform gradient: Cyan → Green → Yellow → Red), `palettes/clear-black.json` (black-first palette useful when the buffer clear color should read as black for Fill **BlendOver**), themed sets such as `pine-forest`, `jungle`, `miami-1984`, and `radioactive`, UI-oriented themes `bill` (Windows 95–inspired), `ddos` (DOS/CGA-inspired), and `blue-pill` (Matrix-inspired), and others. You can add more `.json` files; each file is one palette. Press **P** to cycle through all available palettes when using a palette-aware visualizer; the change applies only to the current visualizer and is saved to that visualizer's settings.

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

## Sample OBJ models (AsciiModel)

The **AsciiModel** text layer loads **Wavefront OBJ** files from a folder. The app ships **`models/sample/`** next to the executable (`cube.obj`, `tetrahedron.obj`; see `models/sample/README.md`). In the S modal, set **Model folder** to that directory (e.g. `models\sample` when running from the build output folder). Press **I** to cycle `.obj` files in alphabetical order. **Ambient**, **Lighting preset** (Classic, Headlight, or Custom angles), and related options reduce overly dark sides; details are in [visualizers/ascii-model.md](visualizers/ascii-model.md).

## Settings structure (`appsettings.json`)

- **UI settings** (`UiSettings`, per [ADR-0033](adr/0033-ui-principles-and-configurable-settings.md), [ADR-0065](adr/0065-ui-theme-from-layer-palettes.md)): App title, optional `TitleBarAppName` (short name for title bar, e.g. "aUdioNLZR"), optional `DefaultAssetFolderPath` (default base directory for AsciiImage / AsciiModel when the layer’s folder setting is empty; omit to use the app content root), optional `UiThemePaletteId` (palette filename without extension — when set, UI and title bar colors are derived from that layer palette file; omit or use General settings **(Custom)** for inline colors), optional `TitleBarPalette` (used when no theme id), UI palette (Normal, Highlighted, Dimmed, Label, optional Background), and default scrolling speed. Stored in `appsettings.json`. Colors support 24-bit RGB (`R`, `G`, `B`) or console color names (`Value`).

- **Visualizer-specific options** live under `VisualizerSettings` in the settings file (e.g. `appsettings.json`):
  - **Presets**: Stored as individual JSON files in the `presets/` directory (see above). `ActivePresetId` references the active preset. Press **V** to cycle presets (Preset editor); **S** to edit (R rename, N new preset).
  - **Shows**: Stored in the `shows/` directory. `ActiveShowId` and `ApplicationMode` (PresetEditor or ShowPlay) determine current mode. **Tab** switches modes; **S** in Show play opens Show edit.

Example `appsettings.json`:

```json
{
  "UiSettings": {
    "Title": "AUDIO ANALYZER - Real-time Frequency Spectrum",
    "DefaultScrollingSpeed": 0.25,
    "UiThemePaletteId": null,
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
