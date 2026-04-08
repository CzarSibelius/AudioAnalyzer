# ADR-0066: BPM source (audio, demo, Ableton Link) and native Link shim

**Status**: Accepted

## Context

Users want tempo and beat alignment from **Ableton Link** (e.g. Rekordbox on the LAN) while the app continues to analyze **spectrum and level from the audio device**. Beat detection was previously always energy-based on the captured stream. Ableton Link is provided as **C++** (GPL-2.0+); the managed app needs a small native boundary.

## Decision

1. **`BpmSource`** (`AppSettings`, persisted in `appsettings.json`): `AudioAnalysis` (energy-based `BeatDetector`), `DemoDevice` (fixed BPM from `demo:` device id + time-based beat grid), `AbletonLink` (tempo and beat from Link via native shim).
2. **`IBeatTimingSource`** in Application supplies `CurrentBpm`, `BeatCount`, `BeatFlashActive`, and `BeatSensitivity` (meaningful for audio analysis only). **`AnalysisEngine`** always computes FFT, waveform, and volume from audio buffers; beat fields come only from the active timing source.
3. **`BeatTimingRouter`** implements `IBeatTimingSource` and **`IBeatTimingConfigurator`**: applies settings + device id, enables/disables Link networking when switching modes.
4. **Native**: `native/link-shim` builds `link_shim.dll` against a vendored clone of [Ableton/link](https://github.com/Ableton/link) at `native/third_party/link`. **`LinkSessionNative`** in Infrastructure loads the DLL from the app base directory; if missing, Link mode shows a clear header hint and tempo stays 0.
5. **Visual tick**: `AnalysisEngine.PulseBeatVisualIfDue()` debounces ~50 ms and is invoked from audio processing and **`VisualizationOrchestrator.RefreshHeaderIfNeeded`** so Demo/Link advance even when the header refresh path runs without a matching audio tick.
6. **UI**: General Settings hub row **BPM source (Enter)** cycles the enum. **+/-** beat sensitivity applies only when `BpmSource` is `AudioAnalysis`.
7. **Licensing**: The **Audio Analyzer** source tree (managed code and shared docs) is under **GNU GPL v3.0 only** (`GPL-3.0-only`); see root `LICENSE`. **Ableton Link** and **`native/link-shim`** remain **GPL-2.0+** when built or distributed; cloning Link into `native/third_party/link` is subject to upstream terms. Distributing binaries that include `link_shim.dll` triggers **GPL-2.0+** obligations for that artifact (source offer, etc.). **Maintainer releases omit** `link_shim.dll`; users who want Link build the shim locally. See `NOTICE` and README.

## Consequences

- Spectrum-driven layers need no changes: they read `AudioAnalysisSnapshot` magnitudes/waveform from audio; beat-synced layers follow `BeatCount` / BPM from the selected source.
- CI and developers without CMake still build; Link features activate when `link_shim.dll` is copied next to the executable.
- New persisted field: `BpmSource`. Missing property on load defaults to `AudioAnalysis` per [ADR-0029](0029-no-settings-migration.md).

## Related

- [Ableton Link documentation](https://ableton.github.io/link/)
- [ADR-0024](0024-analysissnapshot-frame-context.md) (snapshot carries frame context)
- [ADR-0061](0061-general-settings-mode.md) (General Settings hub)
