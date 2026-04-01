# Audio Analyzer

**Audio Analyzer** is a Windows console app that listens to audio—what is playing on the PC (loopback), a chosen capture device, or a built-in **Demo Mode** when you want visuals without real audio—and shows real-time level and spectrum analysis. It uses FFT and beat/BPM from your choice of **source**: energy detection on the audio stream, fixed tempo for Demo mode, or **[Ableton Link](https://ableton.github.io/link/)** when you build and drop **`link_shim.dll`** next to the executable (same LAN as Rekordbox or other Link apps; GPL — see [docs/adr/0066-bpm-source-and-ableton-link.md](docs/adr/0066-bpm-source-and-ableton-link.md)). Spectrum and levels always follow the **audio input**; only tempo/beat counters follow Link or Demo when you select that in **General settings**.

The picture is drawn as **stacked text layers** in the terminal: oscilloscope, spectrum-style bars, fractal zoom, plasma-style backgrounds, fill and blend effects, marquee text, ASCII images and 3D OBJ models, and more. You switch **presets** (layer stacks and palettes) from the keyboard and can chain presets into **shows** for timed or beat-based playback.

## What you need

- **Windows** (WASAPI loopback/capture)
- **[.NET 10](https://dotnet.microsoft.com/download)** SDK or later
- NuGet packages restore automatically when you build (no separate install step)

## How to run

### Visual Studio

1. Open `AudioAnalyzer.sln`
2. Set **AudioAnalyzer.Console** as the startup project and run (F5)

### Command line

From the folder that contains `AudioAnalyzer.sln`:

```bash
dotnet build
dotnet run --project src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj
```

On Windows you can use: `src\AudioAnalyzer.Console\AudioAnalyzer.Console.csproj`

## First run

1. If no input was saved yet, pick **Demo Mode**, **loopback** (system output), or a **capture device** (↑/↓, Enter to confirm, Esc to cancel).
2. Play audio—or stay on Demo Mode—to see the display react.
3. Press **H** for **in-app help**; shortcuts depend on whether you are in Preset editor, Show play, or General settings.

## Screen capture

For bug reports or sharing what you see: press **Ctrl+Shift+E** to write a plain-text snapshot under `screen-dumps` next to the executable. You can also run with `--dump-after N` (seconds, then exit) and optional `--dump-path <dir>`.

## At a glance

- **Audio**: Loopback, device capture, or Demo Mode; settings are saved when you change them.
- **Layers & presets**: Up to nine layers per preset; edit layers and options in the **S** settings flow when in Preset editor.
- **Shows**: Optional auto-cycling through presets (see **Tab** in the app help).
- **Files next to the app**: `presets/`, `shows/`, `palettes/`, optional **`link_shim.dll`** for Ableton Link, and `appsettings.json` are documented in [docs/configuration-reference.md](docs/configuration-reference.md).

## More documentation

- **Developing or automating the repo**: [AGENTS.md](AGENTS.md) and [docs/agents/README.md](docs/agents/README.md)
- **JSON formats and settings**: [docs/configuration-reference.md](docs/configuration-reference.md)
- **Architecture decisions**: [docs/adr/README.md](docs/adr/README.md)

## Tips

- Use **loopback** to visualize system playback; use a **capture device** for microphone or other inputs.
- **Demo Mode** is useful to explore visuals without playing music.
- Meaningful spectrum and level readouts need signal on the chosen input (or Demo Mode).
