# Spec: Audio-derived BPM reset (stale / no capture)

**Repository gates** (agents): `dotnet build .\AudioAnalyzer.sln` with **0 warnings**; `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj`; `dotnet format .\AudioAnalyzer.sln --verify-no-changes`. Use **PowerShell** on Windows.

---

## Blueprint

### Context

Energy-based BPM (`BeatDetector` → `AudioDerivedBeatTimingSource`) retains the last smoothed `CurrentBpm` when playback stops or capture ends. Users expect **unknown tempo** (header `—`, snapshot `0`) when there is no meaningful ongoing beat stream. This spec covers the **Audio analysis** path only; Demo and Ableton Link follow [ADR-0066](../../docs/adr/0066-bpm-source-and-ableton-link.md).

### Architecture

- **Detection:** `src/AudioAnalyzer.Application/BeatDetection/BeatDetector.cs`
- **Wiring:** `src/AudioAnalyzer.Application/BeatDetection/AudioDerivedBeatTimingSource.cs`
- **Engine / capture:** `src/AudioAnalyzer.Application/AnalysisEngine.cs`, `src/AudioAnalyzer.Console/DeviceCaptureController.cs` (notifies `IBeatTimingConfigurator.NotifyAudioCaptureStopped` when capture stops or is replaced/shut down)
- **Router:** `src/AudioAnalyzer.Application/BeatDetection/BeatTimingRouter.cs` — reset must not affect non-audio sources
- **UI:** `src/AudioAnalyzer.Console/Console/HeaderContainerStateUpdater.cs` (already maps `CurrentBpm <= 0` to `—` for audio analysis)
- **Tests:** `tests/AudioAnalyzer.Tests/Application/BeatDetection/`

### Constraints

- Reset behavior applies only when active `BpmSource` is **AudioAnalysis** (router guard for capture notification; stale expiry lives only in the audio energy detector).
- Thread-safety: align with `AnalysisEngine` locking and snapshot rules.
- **Product policy ([PBI-010](../../tasks/PBI-010-reset-bpm-beatcount-beat-marks.md)):** **Option B — full soft-reset** when audio-derived tempo becomes unknown: `BeatDetector` clears beat ordinal, flash, and energy/interval history; `AnalysisEngine.SyncBeatMarksWithTiming` realigns waveform beat marks when `BeatCount` drops.

### Stale threshold

- **No accepted beat** for **`BeatDetector.StaleBeatWindowSeconds` (6.0)** wall-clock seconds clears smoothed BPM (`CurrentBpm == 0`) while buffers may still arrive (silence after music). Value is defined in code and mirrored here.

---

## Contract

### Definition of Done

- After a **stale** condition (no accepted beat for **6 seconds** while buffers may still arrive), `CurrentBpm` is **0** and the header shows **`—`** for audio analysis.
- After **capture stopped** or **replaced/shutdown** (no audio path from the controller), `CurrentBpm` is **0** without waiting for the stale window (via `NotifyAudioCaptureStopped` when the active source is audio analysis).
- Demo and Link modes unchanged by audio-only stale rules and by `NotifyAudioCaptureStopped`.
- When tempo clears, **beat ordinal** (`BeatCount`) resets with the detector; waveform strip beat marks do not retain an offset inconsistent with the cleared counter.
- Repository gates pass.

### Regression guardrails

- With ongoing music and beats, BPM estimation and smoothing behave as before.
- `BeatTimingRouter.ApplyFromSettings` does not leak reset across sources.
- Show / export consumers of `BeatCount` see a fresh ordinal after silence or capture stop (same reset as BPM unknown).

### Scenarios

```gherkin
Scenario: BPM clears after stale beat window
  Given BPM source is Audio analysis
  And a steady tempo was detected and BPM shows a numeric value
  When no beat is accepted for longer than the stale threshold
  Then the BPM cell shows "—"
  And snapshot CurrentBpm is 0

Scenario: BPM clears when capture stops
  Given BPM source is Audio analysis
  And BPM shows a numeric value
  When capture is stopped
  Then the BPM cell shows "—" within one header refresh
  And snapshot CurrentBpm is 0

Scenario: Link and Demo unaffected
  Given BPM source is Ableton Link or Demo device
  When audio analysis would treat the stream as stale
  Then behavior matches ADR-0066 for that source

Scenario: Beat ordinal and waveform marks reset with audio tempo clear
  Given BPM source is Audio analysis
  And BeatCount had advanced and waveform beat marks were recorded
  When audio-derived BPM resets to unknown (stale window or capture stop)
  Then BeatCount is 0
  And waveform beat marks in the snapshot are cleared or realigned for the new run
```
