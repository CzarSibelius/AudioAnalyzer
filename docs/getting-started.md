# Getting started

This page is for **building and running Audio Analyzer from source** on Windows, your **first launch**, and related tips. For who the app is for and typical setups, see [product-audience.md](product-audience.md). For JSON, folders next to the executable, and `appsettings.json`, see [configuration-reference.md](configuration-reference.md).

## Prerequisites

- **Windows** (WASAPI loopback or capture)
- **[.NET 10](https://dotnet.microsoft.com/download)** SDK or later
- NuGet packages restore automatically when you build (no separate install step)

Use **PowerShell** from the repository root for the commands below (paths match [AGENTS.md](../AGENTS.md)).

## Build and run

### Visual Studio

1. Open `AudioAnalyzer.sln` at the repository root.
2. Set **AudioAnalyzer.Console** as the startup project.
3. Run with **F5** (or **Ctrl+F5**).

### Command line

From the folder that contains `AudioAnalyzer.sln`:

```powershell
dotnet build .\AudioAnalyzer.sln
dotnet run --project .\src\AudioAnalyzer.Console\AudioAnalyzer.Console.csproj
```

Contributors: the solution should build with **0 warnings**; tests and format checks are described in [AGENTS.md](../AGENTS.md) and [docs/agents/testing-and-verification.md](agents/testing-and-verification.md).

## First run

1. If no input was saved yet, pick **Demo Mode**, **loopback** (system output), or a **capture device** (↑/↓, **Enter** to confirm, **Esc** to cancel).
2. Play audio—or stay on Demo Mode—to see the display react.
3. Press **H** for **in-app help**; shortcuts depend on whether you are in Preset editor, Show play, or General settings.

## Screen capture

For bug reports or sharing what you see: press **Ctrl+Shift+E** to write a plain-text snapshot under `screen-dumps` next to the executable.

You can also run with `--dump-after N` (seconds, then exit) and optional `--dump-path <dir>`.

## At a glance

- **Audio**: Loopback, device capture, or Demo Mode; settings are saved when you change them.
- **Layers & presets**: Up to nine layers per preset (fewer if you remove layers); add or remove layers with **Insert** / **Delete** in the **S** modal layer list; edit layer options there when in Preset editor.
- **Shows**: Optional auto-cycling through presets (see **Tab** in the app help).
- **Files next to the app**: `presets/`, `shows/`, `palettes/`, `themes/` (UI chrome themes), optional `link_shim.dll` for Ableton Link (build locally; see [docs/agents/native-link-shim-build.md](agents/native-link-shim-build.md) and [ADR-0066](adr/0066-bpm-source-and-ableton-link.md)), and `appsettings.json` — details in [configuration-reference.md](configuration-reference.md). Optional **file logging** (`Logging` in `appsettings.json`, off by default) records errors from the display pipeline to a log file for troubleshooting.

## Tips

- Use **loopback** to visualize system playback; use a **capture device** for microphone or other inputs.
- **Demo Mode** is useful to explore visuals without playing music.
- Meaningful spectrum and level readouts need signal on the chosen input (or Demo Mode).
- In **General settings**, enable **Show render FPS** (or set `ShowRenderFps` in `appsettings.json`) to show smoothed full main-render frame rate on the toolbar (main-loop cadence—typically **≥~60** FPS on capable hosts, not audio callback rate); see [ADR-0067](adr/0067-60fps-target-and-render-fps-overlay.md).
- **Max audio history** (General settings hub, **+**/**−** or **Enter** to type) sets how many seconds of mono waveform feed the **waveform strip** overview (default 60, clamped 5–180; stereo stacked mode keeps a matching **right** channel ring, so waveform memory is about **double** while enabled; see [configuration-reference.md](configuration-reference.md), [ADR-0077](adr/0077-waveform-overview-snapshot.md), [ADR-0078](adr/0078-waveform-strip-stereo-beat-marks-goertzel.md)).

## Ableton Link

Optional **[Ableton Link](https://ableton.github.io/link/)** tempo sync uses a native **`link_shim.dll`** you build and place next to the executable. Official releases from this repository do **not** ship that DLL. Spectrum and levels follow the **audio input**; tempo/beat counters follow Link or Demo when selected in **General settings**. Licensing: see [NOTICE](../NOTICE) and [ADR-0066](adr/0066-bpm-source-and-ableton-link.md).

## License

- **Audio Analyzer** (this repository): [GNU GPL v3.0 only](../LICENSE) (SPDX: `GPL-3.0-only`).
- **Ableton Link** and **link_shim**: GPL-2.0+ when you build or distribute those components — [NOTICE](../NOTICE), [ADR-0066](adr/0066-bpm-source-and-ableton-link.md).
