# Phase 1: External beat sources (no Link yet)

**Outcome:** BPM / `BeatCount` / `BeatFlashActive` / (where applicable) `BeatSensitivity` are supplied by a pluggable **`IBeatTimingSource`** in Application. **`AnalysisEngine`** always computes FFT, waveform, and volume from **audio buffers**; only the **beat timing** path switches by `BpmSource`.

**Prerequisite:** None.

## Domain and persistence

- [x] Add `BpmSource` enum with at least `AudioAnalysis` and `DemoDevice` (add `AbletonLink` in Phase 3, or add now as unused / hidden in UI).
- [x] Add `BpmSource` property to `AppSettings` with default `AudioAnalysis`.
- [x] Extend `FileSettingsRepository` file DTO + `MapToAppSettings` / `UpdateAppSettings` (+ tests if file mapping is covered).
- [x] Wire load/save in `Program.cs` / `AppSettingsPersistence` so the value round-trips.

## Application: timing abstraction

- [x] Define `IBeatTimingSource` (name per ADR) with: `CurrentBpm`, `BeatCount`, `BeatFlashActive`, `BeatSensitivity` (meaningful for audio only), and lifecycle hooks aligned with today’s engine (e.g. per-audio-buffer energy hook + visual tick / flash decay).
- [x] Implement `AudioDerivedBeatTimingSource` wrapping existing `BeatDetector` (delegate `ProcessFrame`-equivalent and `DecayFlashFrame`).
- [x] Implement `DemoBeatTimingSource`: BPM from active `demo:NNN` device id; define beat count / flash policy (time-based vs synthetic kick) and document in ADR or spec.
- [x] Refactor `AnalysisEngine` to take `IBeatTimingSource`; keep FFT/volume path **unconditional** on audio buffers; route beat updates only through the timing source.
- [x] Ensure `GetSnapshot()` / `FillSnapshot()` populate `CurrentBpm`, `BeatCount`, etc. from `IBeatTimingSource`.

## Runtime wiring

- [x] Factory or registration in `ServiceConfiguration` (and runtime swap when device / `BpmSource` changes from shell/hub if applicable).
- [x] Pass current device id (or parsed demo BPM) into factory where Demo timing needs it.

## UI (Phase 1 minimum)

- [x] General Settings hub: row **BPM source** cycling **Audio** / **Demo** only (hide Link until Phase 3), or single setting in appsettings only for a slimmer Phase 1 — **pick one** and update [ui-spec-general-settings-hub.md](../../ui-spec-general-settings-hub.md).
- [x] `HeaderContainerStateUpdater`: label BPM line by source (do not show “Beat: … (+/-)” for Demo).
- [x] `MainLoopKeyHandler`: +/- sensitivity only when `BpmSource == AudioAnalysis`.

## Tests

- [x] Unit tests: `AudioDerivedBeatTimingSource` delegates to `BeatDetector`.
- [x] Unit tests: `DemoBeatTimingSource` BPM and beat progression for representative device ids.

## Documentation

- [x] New or extended ADR: external BPM source, snapshot contract (audio analysis vs beat timing), [ADR-0029](../../adr/0029-no-settings-migration.md) default for new enum.
- [x] `docs/configuration-reference.md`: `BpmSource` JSON values.
- [x] Update [docs/adr/README.md](../../adr/README.md) index.

## Spectrum / full-audio visualizers (Phase 1 verification)

- [x] Confirm no visualizer changes required: they keep reading `AudioAnalysisSnapshot.SmoothedMagnitudes`, waveform, volume from audio processing.
- [x] Add a short note to ADR or [docs/agents/visualizers.md](../../agents/visualizers.md): beat fields may be **decoupled** from spectrum when `BpmSource != AudioAnalysis`.
