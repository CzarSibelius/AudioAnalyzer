# PBI-019: Feature capability report (startup log + General Settings status + macOS permission preflight)

**Transient work item** — close after merge. **The Spec** (`specs/console-ui/general-settings-hub/spec.md`) holds the **UI state**; this file holds the **delta**. Decision: [ADR-0095](../docs/adr/0095-feature-capability-report.md).

## Directive

Give operators a clear, consolidated view of which shim/permission-dependent features are functional and which are not — at startup (log) and in the General Settings hub (read-only status). Implement [ADR-0095](../docs/adr/0095-feature-capability-report.md).

### Phase 1 — Capability report abstraction + per-platform probes + startup log

1. **Application abstractions** (`src/AudioAnalyzer.Application/Abstractions/`, one type per file per ADR-0016):
   - `FeatureCapabilityStatus` record (`Id`, `Name`, `Availability`, `Detail`, `Category`).
   - `FeatureAvailability` enum (`Available`, `Unavailable`, `NotApplicable`).
   - `FeatureCapabilityCategory` enum (`Audio`, `Visual`, `Integration`, `Permission`).
   - `IFeatureCapabilityProbe` (per-platform contributor: returns zero or more statuses).
   - `IFeatureCapabilityReport` (aggregates probes; returns an ordered, cached snapshot; provides an explicit refresh).
2. **Probes** reuse existing booleans — do not duplicate detection:
   - Cross-platform/managed: Ableton Link (`ILinkSession.IsAvailable`), screen dump (`IScreenDumpContentProvider` presence/null), audio capture (host enumeration success).
   - macOS (`src/AudioAnalyzer.Platform.macOS/`): system-audio tap (`MacOsCoreAudioTapAvailability.IsCaptureReady`), webcam (`MacOsCameraCaptureAvailability.IsCaptureReady`), now-playing (`MacOsMediaRemoteAdapterAvailability.IsAvailable`), Core Audio mic.
   - Windows (`src/AudioAnalyzer.Platform.Windows/`): WASAPI capture, GSMTC now-playing, MediaCapture webcam, Link `link_shim.dll` probe, screen dump.
   - Register probes via the per-platform DI modules ([ADR-0092](../docs/adr/0092-platform-behavior-via-abstractions-and-di-module.md)); register `IFeatureCapabilityReport` in `ServiceConfiguration`.
3. **Startup log** in `Program.cs` (alongside `IPlatformStartupDiagnostics.LogStartup()` / `StartupLogging`): one Information **summary** line + one Information line **per capability**, using `LoggerMessage.Define` / `[LoggerMessage]`. Honors [ADR-0076](../docs/adr/0076-configurable-application-logging.md).

### Phase 2 — macOS permission preflight (non-prompting)

4. **Native exports (status only, never prompt):**
   - `native/audio-tap-shim/audio_tap_shim.mm`: export a non-prompting *System Audio Recording* status fn (reuse `TCCAccessPreflight(kAudioCaptureService, nullptr)`; do **not** call `TCCAccessRequest`).
   - `native/video-capture-shim/video_capture_shim.mm`: export a non-prompting *Camera* status fn (`AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo`; do **not** call `requestAccessForMediaType`).
   - *Microphone* status via `AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeAudio` (shared AVFoundation query; place in whichever shim/managed wrapper is cleanest).
5. **Managed wrappers + permission probes** in `src/AudioAnalyzer.Platform.macOS/`: surface each as a `Permission`-category `FeatureCapabilityStatus` (`permission-system-audio`, `permission-microphone`, `permission-camera`); map `Authorized` → `Available`, else `Unavailable` with a `Detail` hint. Windows reports these as `NotApplicable` (or omits them). Rebuild native artifacts via the existing pack-bundle flow (no new bundle artifacts).

### Phase 3 — General Settings hub status section

6. **Read-only "Feature status" section** below the selectable rows in `GeneralSettingsHubAreaRenderer` (`HorizontalRowComponent` lines, preformatted ANSI via `GeneralSettingsHubMenuLines.FormatFeatureStatusLine(...)`). **Not selectable**: excluded from `GeneralSettingsHubMenuRows.Count` and `MoveSelection`; no Enter handler. Hide `NotApplicable` capabilities. Render from the report **snapshot** (refresh on entering Settings mode, not per frame). Draw the transient **Edit:** line between the last menu row and this section.
7. **Update the spec** `specs/console-ui/general-settings-hub/spec.md` — regenerate the screenshot from a real screen dump and confirm the line reference (lines 13–14) match.

**In scope:** the abstractions, probes, DI wiring, startup log, macOS permission preflight (native + managed), the hub status section, and the spec/ADR/index updates.

**Out of scope:** triggering/raising any TCC consent prompt from the report or the hub (prompting stays at capture start per [ADR-0091](../docs/adr/0091-macos-tap-explicit-consent-output-driven-aggregate-stable-signing.md)); a "fix it" / "open System Settings" action button (could be a later PBI); making any status row selectable or editable; a Windows startup-diagnostics rewrite beyond emitting the capability log; persisting capability state.

## Context pointer

- Primary spec: [`specs/console-ui/general-settings-hub/spec.md`](../specs/console-ui/general-settings-hub/spec.md)
- Hub: [`specs/console-ui/spec.md`](../specs/console-ui/spec.md)
- ADRs: [ADR-0095](../docs/adr/0095-feature-capability-report.md) (this decision), [ADR-0092](../docs/adr/0092-platform-behavior-via-abstractions-and-di-module.md), [ADR-0076](../docs/adr/0076-configurable-application-logging.md), [ADR-0083](../docs/adr/0083-concurrent-process-instances-and-log-files.md), [ADR-0091](../docs/adr/0091-macos-tap-explicit-consent-output-driven-aggregate-stable-signing.md), [ADR-0061](../docs/adr/0061-general-settings-mode.md), [ADR-0062](../docs/adr/0062-application-mode-classes.md), [ADR-0069](../docs/adr/0069-unified-menu-selection-affordance.md), [ADR-0030](../docs/adr/0030-performance-priority.md), [ADR-0016](../docs/adr/0016-csharp-documentation-and-file-organization.md), [ADR-0040](../docs/adr/0040-dependency-injection-preference.md)

## Verification pointer

- Contract: **Definition of Done**, **Regression guardrails**, **Scenarios** in the general-settings-hub spec (screenshot matches a fresh dump; every status line has a line-reference entry).
- Unit tests (mirror production layout per [ADR-0064](../docs/adr/0064-test-project-mirrors-production-layout.md)): aggregation/ordering of `IFeatureCapabilityReport` from fake probes; `Availability` mapping (incl. macOS permission status → enum); a fake-probe test asserting the hub status section is non-selectable (`MoveSelection` never lands on it) and `NotApplicable` rows are hidden.
- Build / test / format: root [`AGENTS.md`](../AGENTS.md) — `dotnet build` (0 warnings), tests, `dotnet format --verify-no-changes`. macOS host: pass the pinned `-f net10.0-macos26.0` TFM and rebuild native shims; verify permission preflight does **not** raise a prompt.

## Refinement rule

If implementation reveals a better capability set, log format, status-line layout, or permission-query approach than the spec/ADR describe, **update** [`general-settings-hub/spec.md`](../specs/console-ui/general-settings-hub/spec.md) (and [ADR-0095](../docs/adr/0095-feature-capability-report.md) if the decision itself changes) **in the same commit** (same-commit rule). If the change is product-level or ambiguous, stop and flag for human review.
