# ADR-0018: Shutdown and device-switch lock ordering

**Status**: Accepted

## Context

The console host coordinates audio capture via `IAudioInput` and a `deviceLock`. The main thread runs the UI loop; audio data arrives via `DataAvailable` callbacks on capture threads (NAudio) or thread-pool threads (SyntheticAudioInput). The callback handler acquires `deviceLock` before calling `engine.ProcessAudio()`.

When the user presses ESC to quit or switches devices (D key), the main thread calls `StopCapture()` and `Dispose()` on the current input. If `deviceLock` is held during these calls, a deadlock can occur: the main thread waits for the capture to stop while the capture thread is blocked in the callback waiting for `deviceLock`.

## Decision

**Never hold `deviceLock` across `StopCapture()` or `Dispose()`.** The pattern is:

1. Acquire `deviceLock`, capture a reference to the input, set `currentInput = null` (so in-flight callbacks bail), then release.
2. Call `StopCapture()` and `Dispose()` on the captured reference **without** holding the lock.

This applies to:

- ESC shutdown block in `Program.cs`
- `StartCapture()` when replacing an existing input (device switch)
- Device menu (D key) when stopping capture before showing the menu

## Consequences

- Shutdown and device switches avoid deadlock with the audio callback.
- The handler’s `if (currentInput == null) return` check ensures no work is done after we clear the reference.
- Future code that stops or disposes audio inputs must follow this lock ordering.
