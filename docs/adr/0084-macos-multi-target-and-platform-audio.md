# ADR-0084: macOS multi-targeting and platform-scoped audio implementations

**Status**: Accepted

## Relationship to ADR-0086

[ADR-0086](./0086-macos-windows-hosts-and-screencapturekit.md) supersedes this ADR’s **portable `net10.0` console host** approach for **AudioAnalyzer.Console** and **AudioAnalyzer.Tests**: the product standard is **`net10.0-windows…`** and **`net10.0-macos*`** only. **Platform audio ownership** (WASAPI and macOS adapters not in **Infrastructure**, conditional DI) remains as below; follow **0086** for host TFMs, CI expectations, and optional **ScreenCaptureKit** desktop audio.

## Context

AudioAnalyzer shipped as a Windows-targeted console host (`net10.0-windows…`) with WASAPI enumeration and capture compiled into **AudioAnalyzer.Infrastructure**. That forces Windows-only NAudio/WASAPI types into every consumer of Infrastructure and blocks a clean `net10.0` build on macOS. Operators still need shared layers, presets, Demo mode, and tests on non-Windows hosts without pretending WASAPI exists everywhere.

## Decision

1. **Multi-target the host and tests**  
   - **AudioAnalyzer.Console** and **AudioAnalyzer.Tests** use `TargetFrameworks` with **`net10.0-windows10.0.19041.0`** plus the pinned **`net10.0-macos*`** host TFM from **`Directory.Build.props`** (`AudioAnalyzerMacOsHostTfm`, currently **`net10.0-macos26.0`**) per [ADR-0086](./0086-macos-windows-hosts-and-screencapturekit.md).  
   - **`net10.0-macos*`** (macOS workload): references **`AudioAnalyzer.Platform.macOS`** for platform-selected services.  
   - **`net10.0-windows…`**: full Windows graph; references **`AudioAnalyzer.Platform.Windows`** for WASAPI, now-playing, ASCII webcam, etc.

2. **Conditional project references**  
   Console references **exactly one** platform assembly per target framework (never both in one build). Infrastructure stays referenced by both TFMs but **must not** unconditionally depend on WASAPI-only APIs.

3. **WASAPI leaves Infrastructure**  
   Windows capture (`WasapiCapture`, `WasapiLoopbackCapture`, MMDevice enumeration) and **`WindowsWaveInAudioInput`** live in **`AudioAnalyzer.Platform.Windows`** (e.g. `Audio/` folder). **`SyntheticAudioInput`** (Demo mode) remains in Infrastructure as OS-agnostic. Infrastructure drops the **NAudio** package.

4. **`AudioAnalyzer.Platform.macOS`**  
   Owns macOS adapters for the **`net10.0-macos*`** host build (same TFM pin as Console). **`MacOsAudioDeviceInfo`** lists Demo modes plus **Core Audio** input devices (microphones, aggregate devices, etc.) via **`MacOsCoreAudioEnumerator`** / **`MacOsCoreAudioAudioInput`** (Audio Queue); **`SyntheticAudioInput`** remains for Demo and safe **`null`** device id defaults (ADR consequence below). **System-wide “what you hear” loopback** is **not** provided by the OS like WASAPI; operators use **Demo**, physical inputs, or **virtual audio devices** (e.g. BlackHole) if they need routed/desktop audio—see operator docs.

5. **Dependency injection**  
   **`IAudioDeviceInfo`** registration is chosen at compile time via **`#if WINDOWS`** / **`#elif MACOS`** in **`ServiceConfiguration`** (alongside Windows-only `using`s for now-playing and ASCII video types). Tests inject overrides via **`ServiceConfigurationOptions`** unchanged.  
   **`WINDOWS`** is defined automatically by the .NET SDK for **`net10.0-windows…`** TFMs (no custom **`DefineConstants`** in **`AudioAnalyzer.Console`**); **`MACOS`** is defined for the pinned **`net10.0-macos*`** host build via the Console project file so the macOS platform registration path compiles explicitly.

6. **Cross-compilation**  
   **`EnableWindowsTargeting`** is set on Windows-targeted or multi-target projects that participate in solution restore on non-Windows agents so `dotnet restore` succeeds without building Windows binaries there.

7. **CI**  
   - **Windows** job: full solution build + full test suite (install **`macos`** workload so restore/build resolves the macOS host TFM).  
   - **macOS** job: build and test the pinned **`net10.0-macos*`** configuration (Console + tests project paths), unit tests may exclude **`AudioAnalyzer.Tests.Integration`** initially if documented in workflow comments.

8. **Deferred (unchanged intent)**  
   - **Screen dump**: Win32 buffer reader stays Windows-oriented; Unix/macOS capture remains future work ([ADR-0046](./0046-screen-dump-ascii-screenshot.md)).  
   - **Ableton Link**: native **`dylib`** for macOS remains backlog ([ADR-0066](./0066-bpm-source-and-ableton-link.md)).

## Consequences

- On the **macOS** host TFM, there is **no** OS built-in WASAPI-style loopback; Demo modes must remain functional and defaults must not crash when settings mention Windows-only device ids. **Desktop/output visualization** on macOS uses **virtual routing** plus a dedicated list shortcut per [ADR-0085](./0085-macos-desktop-output-via-virtual-routing.md).
- New OS-specific audio code goes into **`Platform.Windows`** or **`Platform.macOS`**, not Infrastructure.
- Agents and contributors on **macOS** use **`dotnet build … -f net10.0-macos26.0`** / **`dotnet test … -f net10.0-macos26.0`** (or the current pin in **`Directory.Build.props`**) when not building the Windows TFM locally; install **`dotnet workload install macos`** first.
- Path assumptions in tests should use OS-valid roots (e.g. temp paths); shared helpers that derive **file names** from full paths must tolerate **`\`** and **`/`** where tests or assets use mixed separators.
