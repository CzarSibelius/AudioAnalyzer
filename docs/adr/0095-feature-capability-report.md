# ADR-0095: Feature capability report (startup log + General Settings status + macOS permission preflight)

**Status**: Accepted

## Context

Several features depend on **optional native shims** and/or **OS permissions** that may be absent or ungranted at runtime, so a feature can be silently non-functional with no clear signal to the operator:

- **Ableton Link** — `link_shim.dll` (loaded only when present; managed probe is Windows-only today). Status: `ILinkSession.IsAvailable`.
- **macOS system-audio tap** — `libaudio_tap_shim.dylib` **and** the *System Audio Recording* TCC grant. Status: `MacOsCoreAudioTapAvailability.IsCaptureReady` (shim + OS ≥ 14.2). Consent is only discovered implicitly when capture starts (`audio_tap_start`), and Core Audio returns `noErr` even when denied (silent denial).
- **ASCII video / webcam layer** — macOS `libvideo_capture_shim.dylib` + *Camera* TCC; Windows WinRT `MediaCapture` (no shim). Status: `MacOsCameraCaptureAvailability.IsCaptureReady`; Windows has session flags only.
- **Now playing** — macOS `mediaremote-adapter` (vendored Perl binary + `MediaRemoteAdapter.framework`); Windows GSMTC WinRT. Status: `MacOsMediaRemoteAdapterAvailability.IsAvailable`; Windows has none.
- **Screen dump** — Windows `kernel32` console APIs; **unavailable** on macOS (`NullScreenDumpContentProvider`).
- **Core capture (always present on host)** — Windows WASAPI loopback (NAudio) and macOS Core Audio mic/input. Functional in normal operation but worth reporting for a complete "what works" picture; the mic path can be silently denied (*Microphone* TCC) and is only surfaced via a runtime silence watchdog.

Today each capability probes independently. Only the macOS Core Audio tap is logged at startup (`IPlatformStartupDiagnostics.LogStartup()` from `Program.cs`); Windows startup diagnostics are a no-op. Nothing surfaces a consolidated status, and macOS TCC grant state is never preflighted (only requested at capture start, per [ADR-0091](0091-macos-tap-explicit-consent-output-driven-aggregate-stable-signing.md)).

Operators (and support) need to know, at a glance, **which shim/permission-dependent features are functional and which are not, and why**.

## Decision

1. **Cross-platform capability report abstraction.** Add an Application-layer `IFeatureCapabilityReport` that returns an ordered, immutable list of `FeatureCapabilityStatus` records:
   - `Id` (stable key, e.g. `ableton-link`, `system-audio-tap`, `ascii-video`, `now-playing`, `screen-dump`, `audio-capture`, plus macOS permission ids below),
   - `Name` (display text),
   - `Availability` enum: `Available`, `Unavailable`, `NotApplicable` (feature not relevant on this host/OS),
   - `Detail` (short reason / hint, e.g. "no native link_shim.dll", "System Audio Recording not granted"),
   - `Category` enum: `Audio`, `Visual`, `Integration`, `Permission`.

   Platform-specific facts come from per-platform contributors (`IFeatureCapabilityProbe`) composed via DI per [ADR-0092](0092-platform-behavior-via-abstractions-and-di-module.md); the report aggregates them. Consumers (startup log, settings hub) stay platform-agnostic. The report **covers optional shim/consent features and the always-present core capture**.

2. **macOS permission preflight (non-prompting).** Report whether the relevant TCC permissions are **granted**, using **status/preflight** queries that never trigger a prompt (prompting remains at capture start per [ADR-0091](0091-macos-tap-explicit-consent-output-driven-aggregate-stable-signing.md)):
   - *System Audio Recording* — `TCCAccessPreflight(kAudioCaptureService, nullptr) == 0` (the primitive already exists in `native/audio-tap-shim/audio_tap_shim.mm`; export a dedicated non-prompting status function).
   - *Microphone* — `AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeAudio`.
   - *Camera* — `AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo` (the primitive already exists in `native/video-capture-shim/video_capture_shim.mm`; export a dedicated non-prompting status function).

   Each is surfaced through a managed availability type and reported as a `Permission`-category capability (e.g. `permission-system-audio`, `permission-microphone`, `permission-camera`) with `Availability` mapped from the authorization status (`Authorized` → `Available`; `Denied`/`Restricted`/`NotDetermined` → `Unavailable` with a `Detail` hint). On Windows these permission rows are `NotApplicable` (or omitted).

3. **Startup Information log.** During bootstrap (the existing `IPlatformStartupDiagnostics` / `StartupLogging` slot in `Program.cs`), emit:
   - one **summary** Information line — e.g. `Feature capabilities: {available} available, {unavailable} unavailable, {notApplicable} n/a`, and
   - one Information line **per capability** — e.g. `Feature capability {Id}: {Availability} ({Detail})`.

   Use the existing `LoggerMessage.Define` / `[LoggerMessage]` source-generated pattern. Lines are written only when file logging is enabled and `MinimumLevel <= Information` per [ADR-0076](0076-configurable-application-logging.md) (default log path is process-scoped per [ADR-0083](0083-concurrent-process-instances-and-log-files.md)).

4. **General Settings hub status display.** Add a **read-only "Feature status" section** to the General Settings hub, rendered below the selectable menu rows. These lines are **not selectable** and are **not** part of `GeneralSettingsHubMenuRows.Count` navigation (same model as the transient `Edit:` line). Each line reads `name:available/unavailable` with an optional dimmed detail; `NotApplicable` capabilities are hidden. See `specs/console-ui/general-settings-hub/spec.md`.

5. **Performance.** Capability probing runs at startup and is **cached as a snapshot**; the settings hub renders from a snapshot refreshed on entering Settings mode (or on an explicit refresh), **not** probed per frame, per [ADR-0030](0030-performance-priority.md).

## Consequences

- New Application abstractions (`IFeatureCapabilityReport`, `IFeatureCapabilityProbe`, `FeatureCapabilityStatus`, enums) and per-platform probe implementations registered via the platform DI modules ([ADR-0092](0092-platform-behavior-via-abstractions-and-di-module.md)). Existing booleans (`ILinkSession.IsAvailable`, `MacOsCoreAudioTapAvailability`, `MacOsCameraCaptureAvailability`, `MacOsMediaRemoteAdapterAvailability`) are reused, not duplicated.
- Native shims gain **non-prompting** permission-status exports: `audio-tap-shim` (System Audio Recording preflight) and `video-capture-shim` (camera authorization status); microphone status via a shared AVFoundation query. Bundle packaging (`scripts/macos/pack-bundle.sh`) is unaffected (no new artifacts).
- Permission checks **must never prompt**; they are status/preflight only. Actual consent prompting is unchanged and still occurs at capture start.
- Startup log honors logging config; no new files, no hot-path I/O.
- The settings hub gains a non-selectable status block; `MoveSelection` wrapping is unchanged. Spec screenshot + line reference are regenerated.
- Cross-platform parity: Windows reports Link, audio capture (WASAPI), webcam (MediaCapture), now-playing (GSMTC), screen dump; macOS reports audio capture (Core Audio mic), system-audio tap, webcam, now-playing adapter, screen dump (unavailable), plus the three permission rows.

### Affected areas

- `src/AudioAnalyzer.Application/Abstractions/` — `IFeatureCapabilityReport`, `IFeatureCapabilityProbe`, `FeatureCapabilityStatus`, enums.
- `src/AudioAnalyzer.Console/Program.cs`, `StartupLogging.cs` (and/or `IPlatformStartupDiagnostics` impls) — startup log.
- `src/AudioAnalyzer.Console/GeneralSettingsHub/` — `GeneralSettingsHubAreaRenderer`, `GeneralSettingsHubMenuLines` — status section.
- `src/AudioAnalyzer.Platform.macOS/` + `native/audio-tap-shim/`, `native/video-capture-shim/` — permission preflight exports + managed wrappers + probes.
- `src/AudioAnalyzer.Platform.Windows/` — Windows probes.
- `specs/console-ui/general-settings-hub/spec.md`, `docs/adr/README.md`.
