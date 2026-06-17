# ADR-0087: macOS system audio via Core Audio process taps

**Status**: Accepted

> **Update (current):** Per [ADR-0088](./0088-macos-coreaudio-only-and-signed-app-bundle.md), the Core Audio process tap is now the **sole** "what you hear" path on macOS (ScreenCaptureKit and virtual-routing rows were removed), and the host runs from an **ad-hoc signed `.app` bundle** (not a flat `_CanOutputAppBundle=false` exe) so TCC grants **System Audio Recording** / **Microphone**. Decision §3 (coexistence) and §4 (flat host) below are **historical**; see the Consequences "(current)" notes.
>
> **Update (current — capture mechanism):** The shim drives the tap-backed aggregate device with a **device IOProc** (`AudioDeviceCreateIOProcID` + `AudioDeviceStart`), **not** an Audio Queue. The aggregate is created `private`, and an Audio Queue resolves its `CurrentDevice` through the public HAL device list, so `AudioQueueStart` fails on a private tap-backed aggregate (observed as `AudioQueueStart failed`); an IOProc binds to the `AudioObjectID` directly and avoids that lookup. Decision §1 below still describes the original "Audio Queue input" wording for history.

## Context

[ADR-0085](./0085-macos-desktop-output-via-virtual-routing.md) documents **virtual routing** (e.g. BlackHole) as the primary operator path without extra permissions beyond microphone access for physical inputs. [ADR-0086](./0086-macos-windows-hosts-and-screencapturekit.md) adds optional **ScreenCaptureKit** desktop audio, which requires **Screen Recording** consent and a display-scoped content filter.

Apple documents **Core Audio process taps** (macOS **14.2+**) as the supported way to capture **system / process output** without a virtual driver: create a tap, attach it to a HAL **aggregate device**, and read PCM from that device as an input ([Capturing system audio with Core Audio taps](https://developer.apple.com/documentation/CoreAudio/capturing-system-audio-with-core-audio-taps)). TCC uses **`NSAudioCaptureUsageDescription`** and **System Audio Recording** (often listed under Privacy → Screen & System Audio Recording).

Operators want a **native loopback-like** row in the device list that does not depend on BlackHole and avoids ScreenCaptureKit’s screen-recording scope when possible.

## Decision

1. **Native shim**  
   Ship `native/audio-tap-shim` building **`libaudio_tap_shim.dylib`** with a minimal C ABI (`audio_tap_start` / `audio_tap_stop`, PCM callback + format metadata). Implementation uses **ObjC++** (`CATapDescription`, `AudioHardwareCreateProcessTap`, aggregate device + **Audio Queue** input). The managed solution **builds and tests without** the dylib. On **macOS 14.2+**, the Core Audio tap **device row is always listed** (label distinguishes **System Audio Recording** when the shim loads and `audio_tap_is_supported()` is true vs **build the shim** when the library is missing). **Capture** starts only when the shim is present and permission allows.

2. **Application API**  
   Add **`ISystemAudioCapture`**, **`AudioCaptureOptions`**, and **`AudioCaptureFormat`** in **Application.Abstractions** (macOS-only implementations). **`MacOsCoreAudioTapAudioInput`** implements **`IAudioInput`** for the analysis pipeline; stable list id **`CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio`**.

3. **Coexistence**  
   **Virtual routing** ([ADR-0085](./0085-macos-desktop-output-via-virtual-routing.md)) and **ScreenCaptureKit** ([ADR-0086](./0086-macos-windows-hosts-and-screencapturekit.md)) remain supported. For persisted Windows-style **`InputMode: loopback`** with empty device name on macOS, resolution order after Demo (when listed) is: **Core Audio tap** (when available) → **virtual routing** → **ScreenCaptureKit**.

4. **Permissions and host**  
   **`NSAudioCaptureUsageDescription`** (and related keys) are documented in **`AudioAnalyzer.Console/macOS/Info.plist`** for a future signed bundle. The macOS console host builds as a **flat** executable (`_CanOutputAppBundle=false`); operators use **`dotnet run`** (see [getting-started.md](../getting-started.md)). Post-build copies **`libaudio_tap_shim.dylib`** next to the output when present.

5. **Licensing**  
   Shim sources are **GPL-3.0-only** with the rest of the repo; no third-party native dependencies beyond Apple system frameworks.

## Consequences

- **Host (current):** the macOS console runs from an **ad-hoc signed `.app` bundle** ([ADR-0088](./0088-macos-coreaudio-only-and-signed-app-bundle.md)); `libaudio_tap_shim.dylib` lives in **`Contents/MacOS`** (copied by `scripts/macos/pack-bundle.sh` / `FinalizeMacOsAppBundle`), and `MacOsAudioTapShimNative` searches that path. The historical "next to a flat apphost" placement no longer applies.
- **Wrong host on macOS:** if the Windows audio stack is resolved while running on macOS (mis-invoked multi-target build), the console exits with instructions to run the pinned **`net10.0-macos*`** host (`Program` + `MacOsLaunchDiagnostics`).
- **Bootstrap diagnostics:** on the macOS host, an **Information** log records **Core Audio tap** OS support, **capture readiness** (shim + `audio_tap_is_supported`), **`AppContext.BaseDirectory`**, and the process module directory to verify **`libaudio_tap_shim.dylib`** placement in the bundle.
- **Manual test** on macOS 14.2+ with built dylib and the ad-hoc signed app bundle; automated tests mock factories and resolvers.
- **CMake/clang** required locally to produce the dylib (documented in `native/README.md`).
- **Sole desktop path (current):** there is no ScreenCaptureKit or virtual-routing fallback; if the shim is missing or consent is denied, the device list shows a **build-the-shim** label and capture does not start (operators fall back to Demo or a Core Audio input).
