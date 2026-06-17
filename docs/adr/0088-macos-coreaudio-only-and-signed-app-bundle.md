# ADR-0088: macOS capture is Core Audio only, run from an ad-hoc signed `.app` bundle for TCC

**Status**: Accepted

## Context

macOS capture had grown three overlapping paths: **virtual routing** ([ADR-0085](./0085-macos-desktop-output-via-virtual-routing.md)), optional **ScreenCaptureKit** system audio ([ADR-0086](./0086-macos-windows-hosts-and-screencapturekit.md)), and **Core Audio process taps** ([ADR-0087](./0087-macos-core-audio-tap-system-audio.md)). The tap path supersedes the others for "what you hear": it needs no virtual driver (unlike routing) and avoids ScreenCaptureKit's display-scoped content filter and Screen Recording scope. Keeping all three increased device-list noise, resolver branches, and test/maintenance surface for little operator benefit.

The reported **"no capture"** symptom had a separate root cause: the macOS console shipped as a **flat, unsigned executable** (`_CanOutputAppBundle=false`) launched via `dotnet exec`. macOS **TCC** (privacy) only grants **Microphone** / **System Audio Recording** to a process with a stable, code-signed **bundle identity** and embedded **usage strings**; a flat muxer-style host never receives consent, so capture silently produced silence regardless of which path was selected.

Key build finding: **`UseAppHost=true` does not** produce a bound macOS launcher on the macOS workload — it yields a generic dotnet-muxer-like host. The macOS SDK's own `.app` (`_CanOutputAppBundle=true`) **does** produce a proper bound launcher at `Contents/MacOS/AudioAnalyzer.Console`, with managed assemblies in `Contents/MonoBundle` and content under `Contents/Resources`. The codebase already expects this layout (`HostContentPaths` resolves `Contents/Resources`; `MacOsAudioTapShimNative` searches `Contents/MacOS` for `libaudio_tap_shim.dylib`).

## Decision

1. **Two capture paths only**
   macOS keeps **Core Audio process-tap system audio** ([ADR-0087](./0087-macos-core-audio-tap-system-audio.md)) and **Core Audio microphone / input** ([ADR-0084](./0084-macos-multi-target-and-platform-audio.md)). **Remove ScreenCaptureKit** (input, factory, interface, stream-output handler) and **virtual-routing** (the `Desktop / system output` synthetic row, `MacOsDesktopVirtualRouting` id, and the desktop-mix sink heuristic + its tests). `CrossPlatformAudioDeviceIds` keeps only `MacOsCoreAudioTapSystemAudio`. The macOS device list is **Demo modes → Core Audio tap row (14.2+) → Core Audio physical inputs**.

2. **Build the SDK `.app` bundle (not a flat host)**
   The macOS console sets **`_CanOutputAppBundle=true`** so the SDK emits and ad-hoc signs a real bundle with a bound launcher. We run the **inner launcher directly in the terminal** (`Contents/MacOS/AudioAnalyzer.Console`), not `open`, so the interactive TUI keeps the terminal. `SetMacOsFlatRunCommand` points `dotnet run` at that launcher.

3. **Finalize the bundle for TCC**
   The SDK does **not** embed our privacy usage strings or the tap shim. `scripts/macos/pack-bundle.sh` finalizes the SDK bundle: inject **`NSAudioCaptureUsageDescription`** and **`NSMicrophoneUsageDescription`** from `src/AudioAnalyzer.Console/macOS/Info.plist`, copy `libaudio_tap_shim.dylib` into `Contents/MacOS`, then **re-sign ad-hoc** (`codesign --force --deep --sign - --identifier dev.audioanalyzer.console`). The MSBuild target **`FinalizeMacOsAppBundle`** (`BeforeTargets="Run"`) runs the script, and `scripts/macos/run.sh` does build + finalize + exec for a one-command run.

4. **Ad-hoc signing**
   Signing is **ad-hoc** (`codesign --sign -`), sufficient for local TCC grants without an Apple Developer identity. **Caveat:** an ad-hoc signature changes when the bundle is rebuilt, so macOS may **re-prompt or require re-granting** Microphone / System Audio Recording after rebuilds; operators may need to re-toggle consent in **System Settings → Privacy & Security**.

## Consequences

- **Removed code/tests:** `MacOsScreenCaptureKitSystemAudioInput[.Logging].cs` + factory + interface, `MacOsSckStreamOutputHandler.cs`, `MacOsDesktopMixSinkHeuristic.cs` + its test. SCK/virtual-routing branches removed from `CrossPlatformAudioDeviceIds`, `MacOsAudioDeviceInfo(.Logging)`, `MacOsCoreAudioEnumerator`, `DeviceResolver`, `ServiceConfiguration(Options)`, and `MacOsLaunchDiagnostics`; `MacOsAudioDeviceInfoTests` and `DeviceResolverTests` rewritten.
- **Running:** Use `dotnet run -f net10.0-macos26.0` (triggers `FinalizeMacOsAppBundle`) or `scripts/macos/run.sh`. The flat output dir is no longer a supported run target; the shim is delivered **into the bundle** by finalize (the former `CopyAudioTapShimToMacOsOutput` flat-copy target is removed as redundant).
- **Verification:** `codesign --verify --strict` passes on the finalized bundle (`Identifier=dev.audioanalyzer.console`, `Signature=adhoc`); `Contents/Info.plist` carries both usage strings; `Contents/MacOS/libaudio_tap_shim.dylib` is present and sealed; the launcher runs headless (`--dump-after`) and exits cleanly.
- **Supersedes:** [ADR-0085](./0085-macos-desktop-output-via-virtual-routing.md) (virtual-routing row/heuristic dropped) and the **ScreenCaptureKit** portion of [ADR-0086](./0086-macos-windows-hosts-and-screencapturekit.md) (host-TFM policy in 0086 stays in force). [ADR-0087](./0087-macos-core-audio-tap-system-audio.md) is updated for the signed-bundle host and as the sole "what you hear" path.
- **Docs:** getting-started, README, product-audience, `specs/platform-macos`, `specs/console-ui/device-selection-modal`, the Platform.macOS project-structure doc, `native/README.md`, and `.cursor/rules/adr.mdc` updated to describe the signed `.app` run flow and the ad-hoc TCC re-grant caveat.
