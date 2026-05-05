# ADR-0084: macOS multi-targeting and platform-scoped audio implementations

**Status**: Accepted

## Context

AudioAnalyzer shipped as a Windows-targeted console host (`net10.0-windows…`) with WASAPI enumeration and capture compiled into **AudioAnalyzer.Infrastructure**. That forces Windows-only NAudio/WASAPI types into every consumer of Infrastructure and blocks a clean `net10.0` build on macOS. Operators still need shared layers, presets, Demo mode, and tests on non-Windows hosts without pretending WASAPI exists everywhere.

## Decision

1. **Multi-target the host and tests**  
   - **AudioAnalyzer.Console** and **AudioAnalyzer.Tests** use `TargetFrameworks` with **`net10.0`** plus **`net10.0-windows10.0.19041.0`**.  
   - **`net10.0`**: portable graph used on macOS/Linux CI and local Unix shells; references **`AudioAnalyzer.Platform.macOS`** for platform-selected services (audio stub/foundation today).  
   - **`net10.0-windows…`**: full Windows graph; references **`AudioAnalyzer.Platform.Windows`** as today for WASAPI, now-playing, ASCII webcam, etc.

2. **Conditional project references**  
   Console references **exactly one** platform assembly per target framework (never both in one build). Infrastructure stays referenced by both TFMs but **must not** unconditionally depend on WASAPI-only APIs.

3. **WASAPI leaves Infrastructure**  
   Windows capture (`WasapiCapture`, `WasapiLoopbackCapture`, MMDevice enumeration) and **`WindowsWaveInAudioInput`** live in **`AudioAnalyzer.Platform.Windows`** (e.g. `Audio/` folder). **`SyntheticAudioInput`** (Demo mode) remains in Infrastructure as OS-agnostic. Infrastructure drops the **NAudio** package.

4. **`AudioAnalyzer.Platform.macOS`**  
   Owns non-Windows adapters for the **`net10.0`** host. **Foundation milestone**: **`MacOsAudioDeviceInfo`** lists Demo devices and uses **`SyntheticAudioInput`** only; **real Core Audio capture** is follow-up work (separate PBI), documented explicitly so operator docs stay honest.

5. **Dependency injection**  
   **`IAudioDeviceInfo`** registration is chosen at compile time via **`#if WINDOWS`** in **`ServiceConfiguration`** (alongside Windows-only `using`s for now-playing and ASCII video types). Tests inject overrides via **`ServiceConfigurationOptions`** unchanged.  
   **`WINDOWS`** is defined automatically by the .NET SDK for **`net10.0-windows…`** TFMs (no custom **`DefineConstants`** in **`AudioAnalyzer.Console`**); **`net10.0`** builds omit it so the macOS platform registration path compiles.

6. **Cross-compilation**  
   **`EnableWindowsTargeting`** is set on Windows-targeted or multi-target projects that participate in solution restore on non-Windows agents so `dotnet restore` succeeds without building Windows binaries there.

7. **CI**  
   - **Windows** job: full solution build + full test suite (unchanged expectations).  
   - **macOS** job: build and test the **`net10.0`** configuration only (Console + tests project paths), unit tests may exclude **`AudioAnalyzer.Tests.Integration`** initially if documented in workflow comments.

8. **Deferred (unchanged intent)**  
   - **Screen dump**: Win32 buffer reader stays Windows-oriented; Unix/macOS capture remains future work ([ADR-0046](./0046-screen-dump-ascii-screenshot.md)).  
   - **Ableton Link**: native **`dylib`** for macOS remains backlog ([ADR-0066](./0066-bpm-source-and-ableton-link.md)).

## Consequences

- On **`net10.0`**, there is **no** system loopback entry until Core Audio capture exists; Demo modes must remain functional and defaults must not crash when settings mention Windows-only device ids.
- New OS-specific audio code goes into **`Platform.Windows`** or **`Platform.macOS`**, not Infrastructure.
- Agents and contributors running macOS use **`dotnet build … -f net10.0`** / **`dotnet test … -f net10.0`** when not building the Windows TFM locally.
- Path assumptions in tests should use OS-valid roots (e.g. temp paths); shared helpers that derive **file names** from full paths must tolerate **`\`** and **`/`** where tests or assets use mixed separators.
