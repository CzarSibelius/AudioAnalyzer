# macOS first-class platform support

## Blueprint

### Context

AudioAnalyzer ships with **Windows** and **macOS** console builds (**`net10.0-windows…`** and **`net10.0-macos*`** per [ADR-0086](../../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md)—no portable **`net10.0`** host). **WASAPI** loopback and capture live in **`AudioAnalyzer.Platform.Windows`**; **Core Audio** process-tap system audio + microphone/input capture live in **`AudioAnalyzer.Platform.macOS`** (ScreenCaptureKit and virtual-routing paths were removed per [ADR-0088](../../docs/adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)); optional **Windows-only** adapters stay in **Platform.Windows** (now-playing, ASCII webcam); **Infrastructure** stays free of unconditional WASAPI types ([ADR-0084](../../docs/adr/0084-macos-multi-target-and-platform-audio.md)).

**First-class macOS** means: the solution **builds and runs on macOS** (Apple Silicon and Intel where supported by .NET), **automated verification** includes a macOS lane, **operator documentation** describes prerequisites and audio routing realistically, and **feature parity** is explicit: core visualization, presets, shows, and Demo mode work; Windows-only capabilities remain **documented degradations** or gain macOS equivalents where justified.

This spec is the **contract** for that program of work. **Host TFMs** are **[ADR-0086](../../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md)**; macOS capture is **Core Audio only** with the console run from an **ad-hoc signed `.app` bundle** for TCC per **[ADR-0088](../../docs/adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)** (system audio via Core Audio process taps, **[ADR-0087](../../docs/adr/0087-macos-core-audio-tap-system-audio.md)**); the Infrastructure vs platform audio split remains **[ADR-0084](../../docs/adr/0084-macos-multi-target-and-platform-audio.md)**; detailed Core Audio work is tracked in **PBI-013**, not duplicated here.

### Architecture

- **Host TFMs**: Console (and tests that compile against it) multi-target **`net10.0-windows…`** and **`net10.0-macos*`** only ([ADR-0086](../../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md)); each TFM references exactly one platform assembly.
- **Audio abstraction**: `IAudioDeviceInfo` / `IAudioInput` remain in Application; **WASAPI-specific** enumeration and capture leave Infrastructure’s unconditional path and live behind a **platform-selected** implementation (registration in `ServiceConfiguration` or a small factory), e.g. `WindowsAudioDeviceInfo` vs `MacOsAudioDeviceInfo` (names illustrative).
- **New platform assembly**: `AudioAnalyzer.Platform.macOS` (folder layout per [docs/agents/project-structure](../../docs/agents/project-structure/AGENTS.md)) holds macOS-specific adapters (audio, and any future native interop). Non-macOS code stays out of that project.
- **Existing Windows project**: `AudioAnalyzer.Platform.Windows` remains the home for WASAPI-adjacent and WinRT adapters already documented in [ADR-0074](../../docs/adr/0074-ascii-video-layer-and-frame-source.md), [ADR-0027](../../docs/adr/0027-now-playing-header.md), etc.
- **Infrastructure**: Shared, OS-agnostic pieces stay here; **NAudio WASAPI-only** types must not be required for a macOS build (conditional compilation or moved types).
- **Screen dump**: `IScreenDumpContentProvider` is abstracted and provided per platform via DI ([ADR-0046](../../docs/adr/0046-screen-dump-ascii-screenshot.md), [ADR-0092](../../docs/adr/0092-platform-behavior-via-abstractions-and-di-module.md)). macOS currently registers `NullScreenDumpContentProvider` (documented degraded behavior: dump unavailable, returns null); a future Unix-terminal implementation could replace it without touching shared code.
- **Ableton Link**: Native shim is Windows DLL today ([ADR-0066](../../docs/adr/0066-bpm-source-and-ableton-link.md)). macOS **may** ship later via `dylib` + CMake; treat as **follow-up** unless ADR-0084 scopes it into the first milestone.
- **CI**: Add **macOS** job(s): at minimum `dotnet build` for the solution or cross-platform subset and **unit tests** excluding Windows-only integration assumptions.

### Constraints

- **Licensing**: NuGet and native dependencies must satisfy [ADR-0075](../../docs/adr/0075-nuget-license-compatibility.md) (GPL-3.0-only distribution).
- **Host OS for the macOS build**: The pinned **`net10.0-macos26.0`** host TFM targets **macOS 26+** for the built console/app bundle (per .NET 10 SDK **TargetPlatformVersion** rules; **`dotnet build`** may list other valid patch versions such as **26.4**). Older macOS versions are not supported for that TFM without changing the pin in **`Directory.Build.props`** and updating this spec plus operator docs in the same change.
- **Toolchain**: Building the macOS host TFM requires the **`.NET macOS` workload** and **full Xcode** (not Command Line Tools only); see [getting-started.md](../../docs/getting-started.md#prerequisites) and https://aka.ms/macios-missing-xcode.
- **No settings migration** for incompatible schema changes: [ADR-0029](../../docs/adr/0029-no-settings-migration.md).
- **Performance**: Console I/O and main-loop behavior remain subject to [ADR-0030](../../docs/adr/0030-performance-priority.md) and [ADR-0067](../../docs/adr/0067-60fps-target-and-render-fps-overlay.md) on macOS terminals where measurable.
- **Product honesty**: macOS has **no WASAPI equivalent** for “system loopback”. The supported "what you hear" path is the **Core Audio process tap** ([ADR-0087](../../docs/adr/0087-macos-core-audio-tap-system-audio.md)), which needs macOS **14.2+**, the built `libaudio_tap_shim.dylib`, **System Audio Recording** consent, and the console running from an **ad-hoc signed `.app` bundle** ([ADR-0088](../../docs/adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)). Operators may still configure a virtual device (e.g. BlackHole) manually and capture it as a normal Core Audio input. The **device list** and **getting-started** text must not imply parity where the OS does not provide it.
- **TCC and signing**: macOS grants Microphone / System Audio Recording only to a process with a stable **code-signed bundle identity** and embedded usage strings. The macOS console builds the SDK `.app` (`_CanOutputAppBundle=true`) and is finalized (usage strings injected, shim copied into `Contents/MacOS`, re-signed) before running the inner launcher. The system-audio tap requests **System Audio Recording** consent **explicitly** (private TCC API) at capture start, because a console host cannot present the implicit Core Audio prompt; the tap aggregate is **driven by the default output device** so its IOProc actually runs. Signing prefers a **stable self-signed identity** (`AUDIOANALYZER_CODESIGN_IDENTITY`, created by `scripts/macos/create-signing-cert.sh`) so consent persists across rebuilds, falling back to **ad-hoc** (whose identity changes on rebuild, requiring re-grant). See **[ADR-0091](../../docs/adr/0091-macos-tap-explicit-consent-output-driven-aggregate-stable-signing.md)**.
- **Default / null device id** (foundation): **`MacOsAudioDeviceInfo.CreateCapture(null)`** uses **Demo synthesis at 120 BPM** so persisted Windows-centric defaults never crash; operators must **not** read “no selection” as **system loopback** (Windows **`null`** → WASAPI loopback). See **ADR-0084** consequences.
- **Startup default** (fresh settings): for Windows-style loopback with an empty device name, **`DeviceResolver`** prefers the **Core Audio system-audio tap** entry, falling back to **Demo**, then the first device. First launch may therefore prompt for **System Audio Recording** consent; if the tap is listed but not capture-ready it stays silent rather than falling back to Demo (Demo remains selectable in the device modal). See **[ADR-0089](../../docs/adr/0089-macos-startup-default-prefers-system-audio-tap.md)**.
- **Non-blocking capture start**: starting/stopping capture (the synchronous native Core Audio tap setup, including the explicit System Audio Recording consent request) runs **off the UI thread**, so startup and device switches stay responsive instead of freezing until the native call returns. Audio begins after a short warm-up; a responsive UI with no audio indicates an environmental cause (consent denied, shim not built, nothing playing), not a hang. See **[ADR-0090](../../docs/adr/0090-async-capture-start-off-ui-thread.md)**.

### Related specs and docs (derivative updates)

When macOS support ships, update in the **same change set** as behavior or positioning changes:

- [docs/product-audience.md](../../docs/product-audience.md) — remove or narrow the “not cross-platform” non-goal; add macOS audience notes.
- [docs/getting-started.md](../../docs/getting-started.md), [README.md](../../README.md), [docs/configuration-reference.md](../../docs/configuration-reference.md) — build/run commands for macOS, audio routing notes.
- [AGENTS.md](../../AGENTS.md) — build/test examples for Unix shells where Windows-only paths are assumed today.
- [specs/console-ui/spec.md](../console-ui/spec.md) — Context wording (“Windows console” → cross-platform terminal where accurate); screen-dump scenarios if capture semantics differ by OS.

---

## Contract

### Definition of Done

- **ADR-0084** is **Accepted** (platform audio ownership, WASAPI out of Infrastructure). **ADR-0086** is **Accepted** for **host TFMs**. **ADR-0088** is **Accepted** for Core-Audio-only capture and the ad-hoc signed `.app` run flow.
- On a **macOS** CI runner (or documented local smoke): `dotnet build` / `dotnet test` succeed with **0 warnings** for the **macOS** host TFM (per [AGENTS.md](../../AGENTS.md); integration filters as today).
- Launch on macOS: **Demo mode** drives analysis and visualization without crashing on the **macOS** host build. **Core Audio microphone (and other input-capable) capture** plus **Core Audio process-tap system audio** are implemented in **`AudioAnalyzer.Platform.macOS`** (PBI-013, [ADR-0087](../../docs/adr/0087-macos-core-audio-tap-system-audio.md)); **OS-level system loopback** parity with Windows WASAPI is **not** claimed—the tap requires macOS 14.2+, the built shim, consent, and a signed bundle—see **Constraints**, [ADR-0088](../../docs/adr/0088-macos-coreaudio-only-and-signed-app-bundle.md), and operator docs.
- **Windows** release artifacts remain buildable and tested (no regression in existing Windows CI expectations unless intentionally consolidated and documented).
- Operator docs state **limitations** (now-playing, ASCII webcam, Link shim, screen dump) per actual implementation.

### Regression guardrails

- Application-layer contracts (`IAudioDeviceInfo`, `IAudioInput`, modals, visualizer pipeline) stay stable unless ADR records a breaking change.
- Demo-mode device IDs and preset/show JSON semantics unchanged unless documented elsewhere.

### Scenarios

```gherkin
Scenario: macOS CI builds the macOS host target
  Given the repository is checked out on macOS with the pinned .NET SDK
  When the developer runs the documented build command for the macOS TFM
  Then the build completes with zero warnings
```

```gherkin
Scenario: Operator runs without physical audio using Demo mode on macOS
  Given the application started on macOS
  When the operator selects a Demo Mode device
  Then analysis and text-layer visualization update without an external audio device
```

```gherkin
Scenario: Core Audio capture normalizes PCM to float32 for analysis on macOS
  Given the application captures from a Core Audio input on macOS
  When the hardware delivers integer PCM (for example 24-bit) that the shared analysis path does not consume directly
  Then the platform adapter converts delivery buffers to IEEE float32 interleaved PCM before feeding analysis so energy and beat detection are not stuck at zero solely due to bit-depth mismatch
```

```gherkin
Scenario: macOS console runs from an ad-hoc signed app bundle for TCC
  Given the macOS host build is finalized via scripts/macos/pack-bundle.sh (or FinalizeMacOsAppBundle)
  When codesign verifies the bundle and its Info.plist is inspected
  Then the bundle satisfies codesign --verify --strict with an ad-hoc signature and identifier dev.audioanalyzer.console
  And Contents/Info.plist contains NSAudioCaptureUsageDescription and NSMicrophoneUsageDescription
  And Contents/MacOS/libaudio_tap_shim.dylib is present when the shim was built
```

```gherkin
Scenario: Operator selects a Core Audio input on macOS
  Given the application started on macOS with the macOS host build
  When the operator selects an enumerated microphone or input device from the device list
  Then capture feeds the analysis pipeline with PCM from Core Audio and visualization reflects live input
```

```gherkin
Scenario: Windows-only features degrade gracefully on macOS
  Given the application is running on macOS
  When a Windows-only capability is invoked or configured
  Then the product does not crash and behavior matches documented degradation or hides the option
```

```gherkin
Scenario: macOS device list offers Core Audio tap on supported OS versions
  Given the application started on macOS 14.2+ with the macOS host build
  When the operator opens the device list
  Then a Core Audio tap desktop audio entry is present per ADR-0087
  And it is the only "what you hear" entry (no ScreenCaptureKit or virtual-routing rows) per ADR-0088
  And the label states that System Audio Recording permission is required when capture is ready, or points operators to build the shim when the dylib is missing
```

```gherkin
Scenario: Loopback default on macOS prefers the Core Audio system-audio tap
  Given persisted settings use Windows-style loopback with an empty device name
  When the macOS device list is resolved at startup
  Then resolution selects the Core Audio system-audio tap entry per ADR-0089
  And it falls back to a Demo entry when no tap entry is present
```

```gherkin
Scenario: Windows build remains healthy after multi-targeting
  Given CI runs on Windows with the Windows target
  When the solution is built and tests are executed
  Then existing Windows behavior and gates remain satisfied
```