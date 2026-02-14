# ADR-0017: Demo mode — synthetic audio input

**Status**: Accepted

## Context

When testing or demonstrating the audio analyzer, users may not have music playing. The visualizers (spectrum, beat circles, oscilloscope, etc.) require an audio stream to show meaningful output. A way to drive the visualizers without real audio would improve the developer and demo experience.

## Decision

We add a **Demo Mode** as a selectable audio input. Demo mode appears in the device selection menu (like loopback or capture devices) and produces a synthetic audio stream:

1. **Synthetic implementation** (`SyntheticAudioInput`): Implements `IAudioInput`; generates 16-bit stereo PCM at 44100 Hz using sine waves (multiple frequencies for spectrum movement) and periodic low-frequency kicks at a configurable BPM. The stream is consumed only by the analysis pipeline; it is not audible.

2. **Device selection integration**: Demo options (e.g. 90, 120, 140 BPM) are exposed as entries in `IAudioDeviceInfo.GetDevices()` with IDs like `demo:120`. `CreateCapture(demo:120)` returns `SyntheticAudioInput` instead of NAudio-based capture.

3. **Settings persistence**: Demo device selection is persisted like other devices (`InputMode` = "device", `DeviceName` = "demo:120"). `DeviceResolver` matches `d.Id == settings.DeviceName` so demo devices resolve correctly on startup.

## Consequences

- Visualizers can be tested and demonstrated without real audio playing.
- No changes to `AnalysisEngine`, visualizers, or the analysis pipeline; synthetic input plugs into the existing `IAudioInput` abstraction.
- Demo mode does not require NAudio capture devices; it works on any environment where the app runs.
- Implementation lives in `AudioAnalyzer.Infrastructure` alongside `NAudioAudioInput` and `NAudioDeviceInfo`.
