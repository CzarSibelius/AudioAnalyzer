# AudioAnalyzer.Platform.macOS — folder layout

**`Audio/`**: **`MacOsAudioDeviceInfo`** — non-WASAPI device listing for the **`net10.0`** console host; Demo synthesis via **`SyntheticAudioInput`** until Core Audio capture ships ([ADR-0084](../../adr/0084-macos-multi-target-and-platform-audio.md)).

## Rules

- Keep Windows-only code out of this project.
- Prefer mirroring the abstraction shape from Application (`IAudioDeviceInfo` → `Audio/` here).
