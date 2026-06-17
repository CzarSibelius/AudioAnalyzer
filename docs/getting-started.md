# Getting started

This page is for **building and running Audio Analyzer from source** on Windows, your **first launch**, and related tips. For who the app is for and typical setups, see [product-audience.md](product-audience.md). For JSON, folders next to the executable, and `appsettings.json`, see [configuration-reference.md](configuration-reference.md).

## Prerequisites

- **Windows** (full **WASAPI** loopback/capture parity).
- **macOS 14.2+** (Apple Silicon or Intel supported by .NET): **Core Audio** input capture + Demo mode, and **Core Audio process-tap** system audio ("what you hear") when you build the native shim and grant **System Audio Recording**. There is **no** OS-provided WASAPI-style loopback; the tap is the supported desktop path ([ADR-0084](adr/0084-macos-multi-target-and-platform-audio.md), [ADR-0087](adr/0087-macos-core-audio-tap-system-audio.md), [ADR-0088](adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)). Capture requires running from an **ad-hoc code-signed `.app` bundle** so macOS privacy (TCC) can grant consent — `dotnet run` and `scripts/macos/run.sh` produce and run it for you (see below). **Shipping host policy** (Windows + macOS TFMs only): [ADR-0086](adr/0086-macos-windows-hosts-and-screencapturekit.md). The repo pins **`net10.0-macos26.0`** in **`Directory.Build.props`** (`AudioAnalyzerMacOsHostTfm`); that TFM implies a **minimum macOS 26** runtime for the built app (matches .NET’s **TargetPlatformVersion** for this pin; if `dotnet build` reports **NETSDK1140**, align the pin with the SDK’s listed valid versions). Building the macOS host requires the **`.NET macOS` workload** (`dotnet workload install macos`) and a **full Xcode** install selected with **`xcode-select`** (not Command Line Tools only)—see [Microsoft’s macOS/iOS build prerequisites](https://aka.ms/macios-missing-xcode).
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
# Build + finalize the ad-hoc signed .app bundle, then run the inner launcher in this terminal:
scripts/macos/run.sh
# (equivalently) dotnet run also finalizes and runs the bundle launcher:
dotnet run --project src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj -f net10.0-macos26.0
```

**Why a `.app` bundle:** macOS privacy (TCC) only grants **Microphone** / **System Audio Recording** to a process with a stable, **code-signed bundle identity**. A flat `dotnet exec` host never gets consent. The macOS console therefore builds the SDK **`.app`** (`_CanOutputAppBundle=true`); a finalize step injects the privacy usage strings from **`src/AudioAnalyzer.Console/macOS/Info.plist`**, copies the tap shim into **`Contents/MacOS`**, and **ad-hoc re-signs** the bundle (`codesign --force --deep --sign - --identifier dev.audioanalyzer.console`). Both `dotnet run` (via the `FinalizeMacOsAppBundle` MSBuild target) and `scripts/macos/run.sh` do this and then run **`…/AudioAnalyzer.Console.app/Contents/MacOS/AudioAnalyzer.Console`** directly so the interactive TUI keeps the terminal ([ADR-0088](adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)).

**Privacy consent:** On first run, accept the **Microphone** / **System Audio Recording** prompts. For system audio the tap requests **System Audio Recording** consent **explicitly** when capture starts (a console host cannot show the implicit Core Audio prompt), so the dialog appears even though the app is a terminal TUI. If macOS does not prompt, enable consent for **AudioAnalyzer** in **System Settings → Privacy & Security** (System Audio Recording may appear under **Screen & System Audio Recording**). **Caveat:** the default **ad-hoc** signature changes whenever you rebuild, so macOS may **re-prompt or require re-toggling** consent after a rebuild — to make consent **persist across rebuilds**, create a stable self-signed identity once with **`scripts/macos/create-signing-cert.sh`** and `export AUDIOANALYZER_CODESIGN_IDENTITY="AudioAnalyzer Local Signing"` before running ([ADR-0091](adr/0091-macos-tap-explicit-consent-output-driven-aggregate-stable-signing.md)).

**System audio (Core Audio tap, macOS 14.2+):** Build **`libaudio_tap_shim.dylib`** ([`native/README.md`](../native/README.md)) so the finalize step can copy it into the bundle’s **`Contents/MacOS`**. The device list always shows the Core Audio tap row on supported macOS versions; the label switches from **build the shim** to **System Audio Recording** when the dylib loads ([ADR-0087](adr/0087-macos-core-audio-tap-system-audio.md)). Check the bootstrap **Information** log for **`capture_ready`** if capture still fails.

**Webcam (ASCII video layer, macOS):** Build **`libvideo_capture_shim.dylib`** ([`native/README.md`](../native/README.md)) so the finalize step copies it into **`Contents/MacOS`** and embeds **`NSCameraUsageDescription`**. Add an **ASCII video** text layer (S modal) and grant **Camera** access when prompted (or enable it for **AudioAnalyzer** in **System Settings → Privacy & Security → Camera**). Without the dylib the layer shows **No camera** and the webcam device list is empty ([ADR-0074](adr/0074-ascii-video-layer-and-frame-source.md)).

Contributors: the solution should build with **0 warnings**; tests and format checks are described in [AGENTS.md](../AGENTS.md) and [docs/agents/testing-and-verification.md](agents/testing-and-verification.md). On macOS CI or local Unix shells, **`dotnet test`** for the macOS graph uses **`-f net10.0-macos26.0`** (same string as **`Directory.Build.props`** → **`AudioAnalyzerMacOsHostTfm`**). The **`net10.0-windows…`** target requires a Windows environment. **Windows** developers doing a **full** `dotnet build` / `dotnet restore` on the solution also need **`dotnet workload install macos`** so the macOS TFM restores.

## First run

1. If no input was saved yet, pick **Demo Mode**, **loopback / system output** (Windows WASAPI), or a **microphone / input device** (↑/↓, **Enter** to confirm, **Esc** to cancel). On **macOS**, pick **Desktop / system audio (Core Audio tap)** when the shim is built (no virtual driver; **System Audio Recording** consent), choose a specific **Core Audio input** (including a manually configured virtual sink such as BlackHole), or use Demo. On **macOS the startup default for fresh settings is the Core Audio system-audio tap** (fallback Demo, then first device), so first launch may prompt for **System Audio Recording** consent ([ADR-0089](adr/0089-macos-startup-default-prefers-system-audio-tap.md)). A **null / missing saved device id** on macOS still defaults to **Demo synthesis** (120 BPM)—do **not** assume “system loopback” ([ADR-0084](adr/0084-macos-multi-target-and-platform-audio.md)).
2. Play audio—or stay on Demo Mode—to see the display react.
3. Press **H** for **in-app help**; shortcuts depend on whether you are in Preset editor, Show play, or General settings.

## Hear what plays on your Mac (desktop / system output)

This matches **Windows loopback** in intent: visualize **the same audio** macOS sends to your speakers or headphones. On macOS 14.2+ the supported way is the **Core Audio process tap** — no virtual driver required ([ADR-0087](adr/0087-macos-core-audio-tap-system-audio.md), [ADR-0088](adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)).

1. Build the native shim **`libaudio_tap_shim.dylib`** ([`native/README.md`](../native/README.md)).
2. Run via **`scripts/macos/run.sh`** (or `dotnet run -f net10.0-macos26.0`) so the ad-hoc signed `.app` bundle is built and the shim is placed in **`Contents/MacOS`**.
3. In Audio Analyzer’s device list, choose **🔉 Desktop / system audio (Core Audio tap)**.
4. Grant **System Audio Recording** when macOS prompts (the tap requests it explicitly at capture start), or enable it for **AudioAnalyzer** in **System Settings → Privacy & Security**. With the default ad-hoc signature you may need to re-grant after a rebuild; create a stable identity (`scripts/macos/create-signing-cert.sh` + `AUDIOANALYZER_CODESIGN_IDENTITY`) to keep the grant across rebuilds ([ADR-0091](adr/0091-macos-tap-explicit-consent-output-driven-aggregate-stable-signing.md)).

**Prefer a virtual driver instead?** You can still install **[BlackHole 2ch](https://existential.audio/blackhole/)** and a **Multi-Output Device** in **Audio MIDI Setup**, set **System Settings → Sound → Output** to that aggregate, and select BlackHole as a normal **Core Audio input** in the device list. The app no longer adds a dedicated virtual-routing row ([ADR-0088](adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)).

If levels stay flat, confirm the shim loaded (bootstrap **Information** log `capture_ready`), that consent is granted, and that audio is actually playing.

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
- Use **loopback** (Windows) to visualize system playback; use a **capture device** or **microphone** for other inputs. On **macOS**, loopback is not automatic—use the **Core Audio tap** (built shim + System Audio Recording, run from the signed `.app`), manually route **virtual audio**, or pick a suitable **Core Audio** input.
- **Demo Mode** is useful to explore visuals without playing music.
- Meaningful spectrum and level readouts need signal on the chosen input (or Demo Mode).
- In **General settings**, enable **Show render FPS** (or set `ShowRenderFps` in `appsettings.json`) to show smoothed full main-render frame rate on the toolbar (main-loop cadence—typically **≥~60** FPS on capable hosts, not audio callback rate); see [ADR-0067](adr/0067-60fps-target-and-render-fps-overlay.md).
- **Max audio history** (General settings hub, **+**/**−** or **Enter** to type) sets how many seconds of mono waveform feed the **waveform strip** overview (default 60, clamped 5–180; stereo stacked mode keeps a matching **right** channel ring, so waveform memory is about **double** while enabled; see [configuration-reference.md](configuration-reference.md), [ADR-0077](adr/0077-waveform-overview-snapshot.md), [ADR-0078](adr/0078-waveform-strip-stereo-beat-marks-goertzel.md)).

## Ableton Link

Optional **[Ableton Link](https://ableton.github.io/link/)** tempo sync uses a native `**link_shim.dll`** you build and place next to the executable. Official releases from this repository do **not** ship that DLL. Spectrum and levels follow the **audio input**; tempo/beat counters follow Link or Demo when selected in **General settings**. Licensing: see [NOTICE](../NOTICE) and [ADR-0066](adr/0066-bpm-source-and-ableton-link.md).

## License

- **Audio Analyzer** (this repository): [GNU GPL v3.0 only](../LICENSE) (SPDX: `GPL-3.0-only`).
- **Ableton Link** and **link_shim**: GPL-2.0+ when you build or distribute those components — [NOTICE](../NOTICE), [ADR-0066](adr/0066-bpm-source-and-ableton-link.md).

