# macOS first-class platform support

## Blueprint

### Context

AudioAnalyzer ships with **Windows** and **macOS** console builds (**`net10.0-windows…`** and **`net10.0-macos*`** per [ADR-0086](../../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md)—no portable **`net10.0`** host). **WASAPI** loopback and capture live in **`AudioAnalyzer.Platform.Windows`**; **Core Audio** (and optionally **ScreenCaptureKit** desktop audio) live in **`AudioAnalyzer.Platform.macOS`**; optional **Windows-only** adapters stay in **Platform.Windows** (now-playing, ASCII webcam); **Infrastructure** stays free of unconditional WASAPI types ([ADR-0084](../../docs/adr/0084-macos-multi-target-and-platform-audio.md)).

**First-class macOS** means: the solution **builds and runs on macOS** (Apple Silicon and Intel where supported by .NET), **automated verification** includes a macOS lane, **operator documentation** describes prerequisites and audio routing realistically, and **feature parity** is explicit: core visualization, presets, shows, and Demo mode work; Windows-only capabilities remain **documented degradations** or gain macOS equivalents where justified.

This spec is the **contract** for that program of work. **Host TFMs** and optional **ScreenCaptureKit** are **[ADR-0086](../../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md)**; the Infrastructure vs platform audio split remains **[ADR-0084](../../docs/adr/0084-macos-multi-target-and-platform-audio.md)**; detailed Core Audio work is tracked in **PBI-013**, not duplicated here.

### Architecture

- **Host TFMs**: Console (and tests that compile against it) multi-target **`net10.0-windows…`** and **`net10.0-macos*`** only ([ADR-0086](../../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md)); each TFM references exactly one platform assembly.
- **Audio abstraction**: `IAudioDeviceInfo` / `IAudioInput` remain in Application; **WASAPI-specific** enumeration and capture leave Infrastructure’s unconditional path and live behind a **platform-selected** implementation (registration in `ServiceConfiguration` or a small factory), e.g. `WindowsAudioDeviceInfo` vs `MacOsAudioDeviceInfo` (names illustrative).
- **New platform assembly**: `AudioAnalyzer.Platform.macOS` (folder layout per [docs/agents/project-structure](../../docs/agents/project-structure/AGENTS.md)) holds macOS-specific adapters (audio, and any future native interop). Non-macOS code stays out of that project.
- **Existing Windows project**: `AudioAnalyzer.Platform.Windows` remains the home for WASAPI-adjacent and WinRT adapters already documented in [ADR-0074](../../docs/adr/0074-ascii-video-layer-and-frame-source.md), [ADR-0027](../../docs/adr/0027-now-playing-header.md), etc.
- **Infrastructure**: Shared, OS-agnostic pieces stay here; **NAudio WASAPI-only** types must not be required for a macOS build (conditional compilation or moved types).
- **Screen dump**: `IScreenDumpContentProvider` today uses the **Windows console screen buffer** ([ADR-0046](../../docs/adr/0046-screen-dump-ascii-screenshot.md)); macOS needs either a **Unix-terminal** implementation (if feasible without brittle hacks) or a **documented** degraded behavior (e.g. dump unavailable with clear UX).
- **Ableton Link**: Native shim is Windows DLL today ([ADR-0066](../../docs/adr/0066-bpm-source-and-ableton-link.md)). macOS **may** ship later via `dylib` + CMake; treat as **follow-up** unless ADR-0084 scopes it into the first milestone.
- **CI**: Add **macOS** job(s): at minimum `dotnet build` for the solution or cross-platform subset and **unit tests** excluding Windows-only integration assumptions.

### Constraints

- **Licensing**: NuGet and native dependencies must satisfy [ADR-0075](../../docs/adr/0075-nuget-license-compatibility.md) (GPL-3.0-only distribution).
- **Host OS for the macOS build**: The pinned **`net10.0-macos26.0`** host TFM targets **macOS 26+** for the built console/app bundle (per .NET 10 SDK **TargetPlatformVersion** rules; **`dotnet build`** may list other valid patch versions such as **26.4**). Older macOS versions are not supported for that TFM without changing the pin in **`Directory.Build.props`** and updating this spec plus operator docs in the same change.
- **Toolchain**: Building the macOS host TFM requires the **`.NET macOS` workload** and **full Xcode** (not Command Line Tools only); see [getting-started.md](../../docs/getting-started.md#prerequisites) and https://aka.ms/macios-missing-xcode.
- **No settings migration** for incompatible schema changes: [ADR-0029](../../docs/adr/0029-no-settings-migration.md).
- **Performance**: Console I/O and main-loop behavior remain subject to [ADR-0030](../../docs/adr/0030-performance-priority.md) and [ADR-0067](../../docs/adr/0067-60fps-target-and-render-fps-overlay.md) on macOS terminals where measurable.
- **Product honesty**: macOS has **no WASAPI equivalent** for “system loopback” without **virtual audio devices** (e.g. BlackHole), **optional ScreenCaptureKit** system audio ([ADR-0086](../../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md)), or other OS APIs; the **device list** and **getting-started** text must not imply parity where the OS does not provide it.
- **Default / null device id** (foundation): **`MacOsAudioDeviceInfo.CreateCapture(null)`** uses **Demo synthesis at 120 BPM** so persisted Windows-centric defaults never crash; operators must **not** read “no selection” as **system loopback** (Windows **`null`** → WASAPI loopback). See **ADR-0084** consequences.

### Related specs and docs (derivative updates)

When macOS support ships, update in the **same change set** as behavior or positioning changes:

- [docs/product-audience.md](../../docs/product-audience.md) — remove or narrow the “not cross-platform” non-goal; add macOS audience notes.
- [docs/getting-started.md](../../docs/getting-started.md), [README.md](../../README.md), [docs/configuration-reference.md](../../docs/configuration-reference.md) — build/run commands for macOS, audio routing notes.
- [AGENTS.md](../../AGENTS.md) — build/test examples for Unix shells where Windows-only paths are assumed today.
- [specs/console-ui/spec.md](../console-ui/spec.md) — Context wording (“Windows console” → cross-platform terminal where accurate); screen-dump scenarios if capture semantics differ by OS.

---

## Contract

### Definition of Done

- **ADR-0084** is **Accepted** (platform audio ownership, WASAPI out of Infrastructure). **ADR-0086** is **Accepted** for **host TFMs** and optional **ScreenCaptureKit** desktop audio.
- On a **macOS** CI runner (or documented local smoke): `dotnet build` / `dotnet test` succeed with **0 warnings** for the **macOS** host TFM (per [AGENTS.md](../../AGENTS.md); integration filters as today).
- Launch on macOS: **Demo mode** drives analysis and visualization without crashing on the **macOS** host build. **Core Audio microphone (and other input-capable) capture** is implemented in **`AudioAnalyzer.Platform.macOS`** (PBI-013); **OS-level system loopback** parity with Windows WASAPI is **not** claimed without user-installed routing or optional SCK—see **Constraints**, [ADR-0085](../../docs/adr/0085-macos-desktop-output-via-virtual-routing.md), [ADR-0086](../../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md), and operator docs.
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
Scenario: macOS device list offers a desktop output shortcut when virtual routing is used
  Given the application started on macOS with the macOS host build
  When the operator opens the device list
  Then a Desktop or system output shortcut entry is present alongside Demo modes and Core Audio inputs per ADR-0085
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
Scenario: macOS device list offers ScreenCaptureKit system audio when supported
  Given the application started on macOS with the macOS host build
  When the operator opens the device list
  Then a ScreenCaptureKit desktop audio entry is present alongside virtual routing and Core Audio inputs per ADR-0086
  And the entry label makes clear that Screen Recording permission is required
```

```gherkin
Scenario: Loopback default prefers virtual routing over ScreenCaptureKit when both list entries exist
  Given persisted settings use Windows-style loopback with an empty device name
  When the device list includes both the virtual routing row and the ScreenCaptureKit row
  Then resolution prefers the virtual routing id before the ScreenCaptureKit id
```

```gherkin
Scenario: Windows build remains healthy after multi-targeting
  Given CI runs on Windows with the Windows target
  When the solution is built and tests are executed
  Then existing Windows behavior and gates remain satisfied
```