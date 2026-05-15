# Getting started

This page is for **building and running Audio Analyzer from source** on Windows, your **first launch**, and related tips. For who the app is for and typical setups, see [product-audience.md](product-audience.md). For JSON, folders next to the executable, and `appsettings.json`, see [configuration-reference.md](configuration-reference.md).

## Prerequisites

- **Windows** (full **WASAPI** loopback/capture parity).
- **macOS** (Apple Silicon or Intel supported by .NET): **Core Audio** input capture + Demo mode; **no** OS-provided “what you hear” loopback like WASAPI unless you add **virtual audio routing** (e.g. BlackHole + Multi-Output) or the in-app **Desktop / system output** row ([ADR-0084](adr/0084-macos-multi-target-and-platform-audio.md), [ADR-0085](adr/0085-macos-desktop-output-via-virtual-routing.md)). **Shipping host policy** (Windows + macOS TFMs only, optional **ScreenCaptureKit**): [ADR-0086](adr/0086-macos-windows-hosts-and-screencapturekit.md). The repo pins **`net10.0-macos26.0`** in **`Directory.Build.props`** (`AudioAnalyzerMacOsHostTfm`); that TFM implies a **minimum macOS 26** runtime for the built app (matches .NET’s **TargetPlatformVersion** for this pin; if `dotnet build` reports **NETSDK1140**, align the pin with the SDK’s listed valid versions). Building the macOS host requires the **`.NET macOS` workload** (`dotnet workload install macos`) and a **full Xcode** install selected with **`xcode-select`** (not Command Line Tools only)—see [Microsoft’s macOS/iOS build prerequisites](https://aka.ms/macios-missing-xcode).
- **[.NET 10](https://dotnet.microsoft.com/download)** SDK or later.
- NuGet packages restore automatically when you build (no separate install step).

Use **PowerShell** on Windows or a Unix shell on macOS from the repository root for the commands below (paths match [AGENTS.md](../AGENTS.md)).

## Build and run

### Visual Studio

1. Open `AudioAnalyzer.sln` at the repository root.
2. Set **AudioAnalyzer.Console** as the startup project.
3. Run with **F5** (or **Ctrl+F5**).

### Command line (Windows, PowerShell)

From the folder that contains `AudioAnalyzer.sln`:

```powershell
dotnet build .\AudioAnalyzer.sln
dotnet run --project .\src\AudioAnalyzer.Console\AudioAnalyzer.Console.csproj
```

### Command line (macOS)

From the repository root:

```bash
dotnet workload install macos
dotnet build AudioAnalyzer.sln -f net10.0-macos26.0
dotnet run --project src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj -f net10.0-macos26.0
```

**Microphone permission:** macOS shows the system microphone prompt only for a process that carries **`NSMicrophoneUsageDescription`** (in `Info.plist`). `dotnet run` hosts your assembly under the shared **`dotnet`** executable, which does not ship that key, so Core Audio may refuse **`AudioQueueSetProperty(CurrentDevice)`** (often **`OSStatus=-66684`**) even though enumeration works. After each **`net10.0-macos26.0`** build on **macOS**, the project emits **`AudioAnalyzer.app`** next to the SDK **`AudioAnalyzer.Console.app`** (same output folder). **Run from Terminal.app** — launching the `.app` from Finder has no keyboard input and will exit with a dialog. For day-to-day use:

```bash
dotnet run --project src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj -f net10.0-macos26.0
```

For the system microphone prompt (physical inputs), run the bundled binary **from Terminal** (not Finder):

```bash
./src/AudioAnalyzer.Console/bin/Debug/net10.0-macos26.0/osx-arm64/AudioAnalyzer.app/Contents/MacOS/AudioAnalyzer.Console
```

Then grant **Microphone** for **AudioAnalyzer** (or the bundle id **`dev.audioanalyzer.console`**) under **System Settings → Privacy & Security → Microphone**. If Gatekeeper blocks an unsigned local build, use **Right‑click → Open** once, or ad‑hoc sign: `codesign -s - --deep AudioAnalyzer.app`.

Contributors: the solution should build with **0 warnings**; tests and format checks are described in [AGENTS.md](../AGENTS.md) and [docs/agents/testing-and-verification.md](agents/testing-and-verification.md). On macOS CI or local Unix shells, **`dotnet test`** for the macOS graph uses **`-f net10.0-macos26.0`** (same string as **`Directory.Build.props`** → **`AudioAnalyzerMacOsHostTfm`**). The **`net10.0-windows…`** target requires a Windows environment. **Windows** developers doing a **full** `dotnet build` / `dotnet restore` on the solution also need **`dotnet workload install macos`** so the macOS TFM restores.

## First run

1. If no input was saved yet, pick **Demo Mode**, **loopback / system output** (Windows WASAPI), or a **microphone / input device** (↑/↓, **Enter** to confirm, **Esc** to cancel). On **macOS**, pick **Desktop / system output** to auto-select a common virtual mixer (e.g. BlackHole) if installed, **Desktop / system audio (ScreenCaptureKit)** if you accept **Screen Recording** and want desktop audio without virtual routing, choose a specific **🔊 … (desktop mix)** input, or use Demo / any **🎤** input. A **null / missing saved device id** on macOS defaults to **Demo synthesis** (120 BPM)—do **not** assume “system loopback” ([ADR-0084](adr/0084-macos-multi-target-and-platform-audio.md)).
2. Play audio—or stay on Demo Mode—to see the display react.
3. Press **H** for **in-app help**; shortcuts depend on whether you are in Preset editor, Show play, or General settings.

## Hear what plays on your Mac (desktop / system output)

This matches **Windows loopback** in intent: visualize **the same audio** macOS sends to your speakers or headphones.

1. Install a **virtual audio driver** that exposes an **input** you can capture (common choice: **[BlackHole 2ch](https://existential.audio/blackhole/)**).
2. Open **Audio MIDI Setup** (Spotlight: “Audio MIDI Setup”). Click **+** → **Create Multi-Output Device**. Enable your **real output** (built-in speakers, display audio, Bluetooth, etc.) **and** **BlackHole 2ch**. Enable **Drift Correction** on **BlackHole** (recommended).
3. **System Settings → Sound → Output**: choose that **Multi-Output Device** so music still plays on your speakers while a copy goes to BlackHole.
4. In Audio Analyzer’s device list, choose **🔊 Desktop / system output (virtual mixer if installed)** (auto-picks a recognized virtual mixer) or pick **🔊 BlackHole 2ch (desktop mix)** directly.

**Without a virtual driver (ScreenCaptureKit):** Choose **🖥️ Desktop / system audio (ScreenCaptureKit — Screen Recording permission)**. Grant **Screen Recording** when macOS prompts; if permission is denied or revoked, capture will not start (see file logging in `appsettings.json`). Use the virtual-mixer row above, pick a Core Audio **🔊** input, or switch to **Demo Mode** instead. Technical note: ScreenCaptureKit requires a display-scoped content filter; AudioAnalyzer forwards **audio samples only** to the analyzer.

If nothing is routed into the virtual input, levels stay flat—confirm step 3 and that other apps are not using a different output device.

## Screen capture

For bug reports or sharing what you see: press **Ctrl+Shift+E** to write a plain-text snapshot under `screen-dumps` next to the executable.

You can also run with `--dump-after N` (seconds, then exit) and optional `--dump-path <dir>`.

## At a glance

- **Audio**: Loopback, device capture, or Demo Mode; settings are saved when you change them.
- **Layers & presets**: Up to nine layers per preset (fewer if you remove layers); add or remove layers with **Insert** / **Delete** in the **S** modal layer list; edit layer options there when in Preset editor.
- **Shows**: Optional auto-cycling through presets (see **Tab** in the app help).
- **Files next to the app**: `presets/`, `shows/`, `palettes/`, `themes/` (UI chrome themes), optional `link_shim.dll` for Ableton Link (build locally; see [docs/agents/native-link-shim-build.md](agents/native-link-shim-build.md) and [ADR-0066](adr/0066-bpm-source-and-ableton-link.md)), and `appsettings.json` — details in [configuration-reference.md](configuration-reference.md). Optional **file logging** (`Logging` in `appsettings.json`, off by default) records errors from the display pipeline to a log file for troubleshooting.

## Tips

- **Bluetooth headphones (macOS):** When macOS opens an **audio input** on a Bluetooth headset (its microphone), the system often switches that device to the **hands-free / headset profile** (narrow-band), which makes **music playback on the same headphones** sound noticeably worse. Prefer a **wired or built-in** mic for capture, use **Demo mode**, or route desktop audio through a **virtual input** (for example BlackHole) so playback can stay on a high-quality output path. **Multi-output devices** and aggregate routing can interact with this behavior—see Apple’s **Audio MIDI Setup** docs if you use a combined output.
- Use **loopback** (Windows) to visualize system playback; use a **capture device** or **microphone** for other inputs. On **macOS**, loopback is not automatic—use **ScreenCaptureKit**, plan **virtual audio** routing, or pick a suitable **Core Audio** input.
- **Demo Mode** is useful to explore visuals without playing music.
- Meaningful spectrum and level readouts need signal on the chosen input (or Demo Mode).
- In **General settings**, enable **Show render FPS** (or set `ShowRenderFps` in `appsettings.json`) to show smoothed full main-render frame rate on the toolbar (main-loop cadence—typically **≥~60** FPS on capable hosts, not audio callback rate); see [ADR-0067](adr/0067-60fps-target-and-render-fps-overlay.md).
- **Max audio history** (General settings hub, **+**/**−** or **Enter** to type) sets how many seconds of mono waveform feed the **waveform strip** overview (default 60, clamped 5–180; stereo stacked mode keeps a matching **right** channel ring, so waveform memory is about **double** while enabled; see [configuration-reference.md](configuration-reference.md), [ADR-0077](adr/0077-waveform-overview-snapshot.md), [ADR-0078](adr/0078-waveform-strip-stereo-beat-marks-goertzel.md)).

## Ableton Link

Optional **[Ableton Link](https://ableton.github.io/link/)** tempo sync uses a native `**link_shim.dll`** you build and place next to the executable. Official releases from this repository do **not** ship that DLL. Spectrum and levels follow the **audio input**; tempo/beat counters follow Link or Demo when selected in **General settings**. Licensing: see [NOTICE](../NOTICE) and [ADR-0066](adr/0066-bpm-source-and-ableton-link.md).

## License

- **Audio Analyzer** (this repository): [GNU GPL v3.0 only](../LICENSE) (SPDX: `GPL-3.0-only`).
- **Ableton Link** and **link_shim**: GPL-2.0+ when you build or distribute those components — [NOTICE](../NOTICE), [ADR-0066](adr/0066-bpm-source-and-ableton-link.md).

