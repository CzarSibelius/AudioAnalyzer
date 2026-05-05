# PBI-013: macOS audio input (enumeration + capture)

**Transient work item** — close after merge.

## Directive

Deliver a **working macOS audio path** behind `IAudioDeviceInfo` / `IAudioInput` so operators can run live analysis (not only Demo mode).

**In scope**

- Implement device enumeration and capture in **`AudioAnalyzer.Platform.macOS`** using the stack chosen in **ADR-0084** (Core Audio / supported binding — Dev validates license per [ADR-0075](../docs/adr/0075-nuget-license-compatibility.md)).
- **Device list UX**: Demo modes preserved; macOS entries reflect real capabilities (microphones, aggregated devices, optional loopback-via-virtual-device — **document** what we can detect).
- **Default device** behavior: sensible fallback when `deviceId` is null; no silent catch blocks ([ADR-0011](../docs/adr/0011-no-empty-catch-blocks.md)).
- Unit tests with **mocked or synthetic** boundaries where possible; mirror layout under `tests/AudioAnalyzer.Tests/` per [ADR-0064](../docs/adr/0064-test-project-mirrors-production-layout.md).

**Out of scope**

- Guaranteed OS-level “what you hear” loopback without user-installed routing — document as limitation unless ADR expands scope.
- Ableton Link native macOS binary — separate backlog unless merged into ADR-0084.

## Context pointer

- Primary spec: [`specs/platform-macos/spec.md`](../specs/platform-macos/spec.md)
- Depends on: [`PBI-012-macos-platform-foundation.md`](./PBI-012-macos-platform-foundation.md)
- Related: [`docs/adr/0017-demo-mode-synthetic-audio.md`](../docs/adr/0017-demo-mode-synthetic-audio.md), [`docs/adr/0066-bpm-source-and-ableton-link.md`](../docs/adr/0066-bpm-source-and-ableton-link.md)

## Verification pointer

- Spec scenarios: Demo mode on macOS (baseline); **real audio path** scenario to extend when capture exists.
- Manual macOS smoke: select enumerated device, confirm FFT/BPM path receives samples (document in PR if no integration test).

## Acceptance criteria

- At least one non-demo capture path works on macOS per ADR-0084.
- Device list and defaults do not imply WASAPI loopback parity where unsupported.

## Refinement rule

If macOS cannot expose loopback IDs analogous to Windows `loopback:name`, update **device-selection** help text and [`specs/console-ui/device-selection-modal/spec.md`](../specs/console-ui/device-selection-modal/spec.md) in the same change.
