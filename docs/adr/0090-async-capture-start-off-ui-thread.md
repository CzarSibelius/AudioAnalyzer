# ADR-0090: Start and stop audio capture off the UI thread

**Status**: Accepted

## Context

`DeviceCaptureController.StartCapture` (and `RestartCapture`) used to call the audio input's
`Start()` — and the previous input's `StopCapture()` / `Dispose()` — synchronously on the calling
thread. The caller is the UI/main thread: `ApplicationShell.Run` starts capture before the first
redraw, and the **D** device-selection key handler and the General Settings hub start capture
inline before redrawing.

On macOS this blocks for a long time. Starting the Core Audio **system-audio tap**
([ADR-0087](0087-macos-core-audio-tap-system-audio.md)) runs native `audio_tap_start`
synchronously: it creates a process tap, builds a private aggregate device, and opens an Audio
Queue, and — because the macOS startup default now auto-selects the tap
([ADR-0089](0089-macos-startup-default-prefers-system-audio-tap.md)) — the **System Audio
Recording** TCC consent prompt is triggered during that call. macOS can hold the calling thread
until the user responds to consent, so the app appeared frozen at startup (and again on device
switch) until the native call returned, after which the UI became responsive but produced no audio
if consent was denied or the tap was not capture-ready. The Core Audio **microphone/input** path
has the same shape (synchronous `AudioQueueNewInput` / `AudioQueueStart`, plus a Microphone TCC
prompt).

This conflicts with [ADR-0030](0030-performance-priority.md) (console writes, polling, and timing
must not block the main loop).

## Decision

`DeviceCaptureController` publishes the selected device id/name synchronously (so the header
reflects the choice immediately) but runs the blocking transition — stop/dispose the previous
input, then `Start()` the new one — on a background thread (`Task.Run`).

A dedicated transition lock serializes these transitions so overlapping device switches cannot run
concurrently (the native tap has a single global state). Each transition re-checks, under the
device lock, that its input is still the current one (`ReferenceEquals`) before applying beat-timing
settings and calling `Start()`; a superseded input is stopped/disposed by the next transition and
never started. The device lock is never held across the blocking `Start()` / `StopCapture()` calls,
preserving the input-lock → device-lock ordering used by capture callbacks
([ADR-0018](0018-shutdown-lock-ordering.md)). Failures in the background transition are logged, not
thrown.

`ReleaseCaptureForDeviceSelection` stays synchronous: device enumeration for the picker requires the
previous capture to be fully released first (a stop, which is fast), and the modal must not open
until that completes.

## Consequences

- **Startup and device switches stay responsive**: the UI renders and accepts input while capture
  initializes in the background, even while macOS shows a TCC consent prompt.
- Audio still begins only once `Start()` completes; there is a short warm-up window after selection
  during which no audio is registered yet. This is expected, not a hang.
- "No audio" after a responsive UI remains a separate, environmental concern (TCC consent denied,
  `libaudio_tap_shim.dylib` not built, or nothing playing). Those are reported via existing logging
  (`MacOsCoreAudioTapAudioInput` tap-unavailable / start-failed messages, the mic-path silence
  watchdog, and bootstrap `capture_ready` logging), not surfaced as a freeze.
- **Affected:** `src/AudioAnalyzer.Console/DeviceCaptureController.cs`,
  `src/AudioAnalyzer.Console/DeviceCaptureController.Logging.cs`.
