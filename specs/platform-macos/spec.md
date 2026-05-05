# macOS first-class platform support

## Blueprint

### Context

AudioAnalyzer ships with a **Windows-targeted** console application (`net10.0-windows…`) plus a **`net10.0`** portable host build; **WASAPI** loopback and capture live in **`AudioAnalyzer.Platform.Windows`** (`WindowsAudioDeviceInfo`), optional **Windows-only** adapters there (now-playing, ASCII webcam), **Infrastructure** stays free of unconditional WASAPI types ([ADR-0084](../../docs/adr/0084-macos-multi-target-and-platform-audio.md)), and documentation that lists **cross-platform as out of scope** is being narrowed as macOS support lands.

**First-class macOS** means: the solution **builds and runs on macOS** (Apple Silicon and Intel where supported by .NET), **automated verification** includes a macOS lane, **operator documentation** describes prerequisites and audio routing realistically, and **feature parity** is explicit: core visualization, presets, shows, and Demo mode work; Windows-only capabilities remain **documented degradations** or gain macOS equivalents where justified.

This spec is the **contract** for that program of work. Multi-target layout and the Infrastructure vs platform audio split are recorded in **[ADR-0084](../../docs/adr/0084-macos-multi-target-and-platform-audio.md)**; detailed Core Audio work is tracked in **PBI-013**, not duplicated here.

### Architecture

- **Host TFMs**: Console (and tests that compile against it) move to **multi-targeting** so `net10.0` builds on macOS while `net10.0-windows…` retains Windows-specific APIs and references. Implementation follows the new ADR once accepted.
- **Audio abstraction**: `IAudioDeviceInfo` / `IAudioInput` remain in Application; **WASAPI-specific** enumeration and capture leave Infrastructure’s unconditional path and live behind a **platform-selected** implementation (registration in `ServiceConfiguration` or a small factory), e.g. `WindowsAudioDeviceInfo` vs `MacOsAudioDeviceInfo` (names illustrative).
- **New platform assembly**: `AudioAnalyzer.Platform.macOS` (folder layout per [docs/agents/project-structure](../../docs/agents/project-structure/AGENTS.md)) holds macOS-specific adapters (audio, and any future native interop). Non-macOS code stays out of that project.
- **Existing Windows project**: `AudioAnalyzer.Platform.Windows` remains the home for WASAPI-adjacent and WinRT adapters already documented in [ADR-0074](../../docs/adr/0074-ascii-video-layer-and-frame-source.md), [ADR-0027](../../docs/adr/0027-now-playing-header.md), etc.
- **Infrastructure**: Shared, OS-agnostic pieces stay here; **NAudio WASAPI-only** types must not be required for a macOS build (conditional compilation or moved types).
- **Screen dump**: `IScreenDumpContentProvider` today uses the **Windows console screen buffer** ([ADR-0046](../../docs/adr/0046-screen-dump-ascii-screenshot.md)); macOS needs either a **Unix-terminal** implementation (if feasible without brittle hacks) or a **documented** degraded behavior (e.g. dump unavailable with clear UX).
- **Ableton Link**: Native shim is Windows DLL today ([ADR-0066](../../docs/adr/0066-bpm-source-and-ableton-link.md)). macOS **may** ship later via `dylib` + CMake; treat as **follow-up** unless ADR-0084 scopes it into the first milestone.
- **CI**: Add **macOS** job(s): at minimum `dotnet build` for the solution or cross-platform subset and **unit tests** excluding Windows-only integration assumptions.

### Constraints

- **Licensing**: NuGet and native dependencies must satisfy [ADR-0075](../../docs/adr/0075-nuget-license-compatibility.md) (GPL-3.0-only distribution).
- **No settings migration** for incompatible schema changes: [ADR-0029](../../docs/adr/0029-no-settings-migration.md).
- **Performance**: Console I/O and main-loop behavior remain subject to [ADR-0030](../../docs/adr/0030-performance-priority.md) and [ADR-0067](../../docs/adr/0067-60fps-target-and-render-fps-overlay.md) on macOS terminals where measurable.
- **Product honesty**: macOS has **no WASAPI equivalent** for “system loopback” without **virtual audio devices** (e.g. BlackHole) or future OS APIs; the **device list** and **getting-started** text must not imply parity where the OS does not provide it.
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

- **ADR-0084** (or superseding number) is **Accepted**: documents multi-target layout, audio backend strategy for macOS, and screen-dump / Link deferrals.
- On a **macOS** CI runner (or documented local smoke): `dotnet build` succeeds with **0 warnings** for the agreed target set; `dotnet test` **unit** suite passes (per [AGENTS.md](../../AGENTS.md); integration filters as today).
- Launch on macOS: **Demo mode** drives analysis and visualization without crashing on the **`net10.0`** host. **Real microphone / loopback capture** on macOS is tracked separately (PBI-013) once ADR-0084 foundation is merged.
- **Windows** release artifacts remain buildable and tested (no regression in existing Windows CI expectations unless intentionally consolidated and documented).
- Operator docs state **limitations** (now-playing, ASCII webcam, Link shim, screen dump) per actual implementation.

### Regression guardrails

- Application-layer contracts (`IAudioDeviceInfo`, `IAudioInput`, modals, visualizer pipeline) stay stable unless ADR records a breaking change.
- Demo-mode device IDs and preset/show JSON semantics unchanged unless documented elsewhere.

### Scenarios

```gherkin
Scenario: macOS CI builds the cross-platform target
  Given the repository is checked out on macOS with the pinned .NET SDK
  When the developer runs the documented build command
  Then the build completes with zero warnings
```

```gherkin
Scenario: Operator runs without physical audio using Demo mode on macOS
  Given the application started on macOS
  When the operator selects a Demo Mode device
  Then analysis and text-layer visualization update without an external audio device
```

```gherkin
Scenario: Windows-only features degrade gracefully on macOS
  Given the application is running on macOS
  When a Windows-only capability is invoked or configured
  Then the product does not crash and behavior matches documented degradation or hides the option
```

```gherkin
Scenario: Windows build remains healthy after multi-targeting
  Given CI runs on Windows with the Windows target
  When the solution is built and tests are executed
  Then existing Windows behavior and gates remain satisfied
```
