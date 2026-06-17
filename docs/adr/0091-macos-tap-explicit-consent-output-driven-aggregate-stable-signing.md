# ADR-0091: macOS tap — explicit System Audio Recording consent, output-driven aggregate, stable signing

**Status**: Accepted

## Context

After the Core Audio process-tap path landed ([ADR-0087](0087-macos-core-audio-tap-system-audio.md)) and the host became a signed `.app` bundle ([ADR-0088](0088-macos-coreaudio-only-and-signed-app-bundle.md)), the **system-audio loopback still produced no audio** on macOS. Diagnosis with native and managed instrumentation found **three** independent defects, none of which surfaced as an error (every Core Audio call returned `noErr`):

1. **Consent was silently denied.** A Core Audio process tap has no public authorization API; consent is *implicit* — `AudioDeviceStart` is supposed to trigger the **System Audio Recording** (TCC `kTCCServiceAudioCapture`) prompt. A console/TUI host has no normal window-server/run-loop presentation context, so macOS suppressed the implicit prompt and denied access **without returning an error**. The tap then delivered nothing. By contrast the webcam path works from the same host because `AVCaptureDevice requestAccessForMediaType:` makes an **explicit** consent request, which the system honors regardless of process UI context.

2. **The aggregate had no IO clock.** The private aggregate was created with only the tap in `kAudioAggregateDeviceTapListKey` (bare UID string) and no sub-device. `AudioHardwareCreateAggregateDevice`, format resolution, `AudioDeviceCreateIOProcID`, and `AudioDeviceStart` all returned `noErr`, but the **IOProc never fired** — a tap-only aggregate has no device to drive an IO cycle.

3. **Ad-hoc signing churn revoked consent on every rebuild.** Ad-hoc signatures ([ADR-0088](0088-macos-coreaudio-only-and-signed-app-bundle.md) §4) change identity per build, so each rebuild required re-granting consent — making the first two defects much harder to diagnose and the app painful to iterate on.

[ADR-0090](0090-async-capture-start-off-ui-thread.md) had already moved the blocking start off the UI thread on the assumption that `AudioDeviceStart` itself shows the prompt; the real prompt trigger is now the explicit request below.

## Decision

1. **Explicit TCC consent in the native shim.** Before creating the tap, `audio_tap_start` calls `EnsureSystemAudioRecordingAuthorized`, which `dlopen`s `TCC.framework` and uses the private `TCCAccessPreflight` / `TCCAccessRequest` for `kTCCServiceAudioCapture`, blocking (up to 30s, off the lock) on a semaphore for the user's response — mirroring the camera path's explicit `requestAccessForMediaType:`. If preflight reports authorized it returns immediately; if the private symbols are unavailable it falls back to the implicit path (returns success). Denied/timed-out returns failure with an actionable message ("Enable AudioAnalyzer in System Settings → Privacy & Security → Screen & System Audio Recording"). This replaces the earlier foreground-promotion (`TransformProcessType`) attempt, which did not make the implicit prompt appear.

2. **Output-driven aggregate.** The private aggregate now includes the current **default output device** as `kAudioAggregateDeviceMainSubDeviceKey` and in `kAudioAggregateDeviceSubDeviceListKey` (`{ kAudioSubDeviceUIDKey }`), and references the tap via the **dictionary** form of `kAudioAggregateDeviceTapListKey` (`{ kAudioSubTapUIDKey, kAudioSubTapDriftCompensationKey: YES }`). The output device supplies the IO clock so the HAL runs an IO cycle and the IOProc fires. If no default output UID is available the aggregate is still created tap-only (best effort).

3. **Stable code-signing identity.** `scripts/macos/create-signing-cert.sh` creates a persistent self-signed code-signing certificate ("AudioAnalyzer Local Signing") in the login keychain. `scripts/macos/pack-bundle.sh` prefers it when `AUDIOANALYZER_CODESIGN_IDENTITY` is set and the identity exists, falling back to ad-hoc otherwise. Because the certificate is self-signed it is **untrusted**, so the lookup uses `security find-identity -p codesigning` (not `-v`); `codesign` signs with it regardless, and **TCC consent persistence depends on the stable certificate, not on the trust chain**. With a stable identity the System Audio Recording grant survives rebuilds.

## Consequences

- **System audio loopback works on macOS.** Verified end-to-end: with consent granted, PCM flows with non-zero peak while audio plays and `peak=0` when silent (managed `MacOsSystemAudioCapture` PCM-activity log, kept for diagnostics).
- **One consent prompt, then persistent.** With the stable identity, the prompt appears once and the grant persists across rebuilds. Without it (ad-hoc fallback) the [ADR-0088](0088-macos-coreaudio-only-and-signed-app-bundle.md) re-grant caveat still applies.
- **Private API dependency.** The explicit consent request uses unsupported `TCC.framework` symbols resolved via `dlopen`/`dlsym`; if they ever disappear the shim degrades to the implicit path rather than failing to load. This is local-only tooling, not redistributed to end users.
- **Amends, does not supersede:** [ADR-0087](0087-macos-core-audio-tap-system-audio.md) (consent is now requested explicitly and the aggregate is output-driven), [ADR-0088](0088-macos-coreaudio-only-and-signed-app-bundle.md) (stable signing is preferred over ad-hoc), and [ADR-0090](0090-async-capture-start-off-ui-thread.md) (the explicit request — not `AudioDeviceStart` — is what blocks for consent, still off the UI thread).
- **Affected:** `native/audio-tap-shim/audio_tap_shim.mm` (explicit consent + output-driven aggregate; `ApplicationServices`/foreground transform removed), `native/audio-tap-shim/CMakeLists.txt` and `native/README.md` (drop `ApplicationServices`), `scripts/macos/create-signing-cert.sh` and `scripts/macos/pack-bundle.sh` (stable identity), `src/AudioAnalyzer.Platform.macOS/Audio/CoreAudioTap/MacOsSystemAudioCapture[.Logging].cs` (PCM-activity diagnostic). The short-lived managed foreground-priming wiring (`audio_tap_activate_foreground` P/Invoke, `ActivateForegroundForConsent`, the `Program.cs` startup call) was added and then removed in favor of the native explicit request.
