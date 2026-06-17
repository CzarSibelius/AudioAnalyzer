# ADR-0089: macOS startup default prefers the Core Audio system-audio tap over Demo

**Status**: Accepted

## Context

On macOS, fresh settings arrive with the Windows default `InputMode=loopback` and an empty
`DeviceName`. There is no OS-provided WASAPI-style loopback, so `DeviceResolver` had to choose a
sensible startup device for that case. It previously preferred a **Demo** entry, only falling back
to the **Core Audio system-audio tap** ([ADR-0087](0087-macos-core-audio-tap-system-audio.md)) when
no Demo entry existed. The rationale (recorded only in a code comment that cited
[ADR-0084](0084-macos-multi-target-and-platform-audio.md)) was to avoid auto-opening the tap — and
its **System Audio Recording** TCC prompt — on first launch.

In practice the macOS product's primary use is **visualizing "what you hear"**, which is exactly the
Core Audio tap path ([ADR-0088](0088-macos-coreaudio-only-and-signed-app-bundle.md)). Defaulting to
Demo meant new operators saw synthetic audio until they manually opened the device modal and selected
the tap row, even though the tap is the headline macOS capability.

## Decision

For macOS fresh settings (`InputMode=loopback` with an empty `DeviceName` and no Windows `null`-id
loopback entry present), `DeviceResolver.TryResolveFromSettings` now prefers, in order:

1. The Core Audio system-audio tap entry (`CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio`).
2. A Demo entry (fallback when the tap row is absent).
3. The first listed device.

Selecting a device in the modal still persists `InputMode=device` + `DeviceName`, so an explicit
later choice (Demo, mic, or tap) overrides this default and is restored on the next launch.

## Consequences

- **First launch on a tap-capable host** auto-selects the tap, so macOS prompts for **System Audio
  Recording** consent right away (per [ADR-0088](0088-macos-coreaudio-only-and-signed-app-bundle.md)
  the console runs from an ad-hoc signed `.app` for TCC). This is the intended "what you hear"
  experience.
- **If the tap is listed but not capture-ready** (shim not built, or consent denied),
  `MacOsCoreAudioTapAudioInput.Start` logs `tap unavailable` and stays silent instead of falling back
  to Demo. Demo remains one keypress away in the device modal (**D**); the tap row label already tells
  operators to build the shim.
- Revises the macOS startup-default behavior previously attributed to
  [ADR-0084](0084-macos-multi-target-and-platform-audio.md). The `null` / missing-device-id default
  is unchanged: `MacOsAudioDeviceInfo.CreateCapture(null)` still uses Demo synthesis (120 BPM).
- **Affected:** `src/AudioAnalyzer.Console/DeviceResolver.cs`,
  `tests/AudioAnalyzer.Tests/Console/DeviceResolverTests.cs`,
  `specs/platform-macos/spec.md`, `docs/getting-started.md`.
- **Related diagnostics cleanup (same change):** on the Core Audio **microphone/input** path
  (`MacOsCoreAudioAudioInput`), the misleading hint that tied `AudioQueueSetProperty(CurrentDevice)`
  `OSStatus=-66684` (`kAudioQueueErr_InvalidProperty`, benign — falls back to default routing) to
  "TCC denied mic access" was removed. A short **silence watchdog** now reports actual silent capture
  (no audio within 3s of `Start`) with status-independent Microphone-permission guidance.
