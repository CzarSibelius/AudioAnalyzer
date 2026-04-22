# PBI-009: Reset audio-derived BPM (stale beats + capture stop)

**Transient work item** — close after merge. **State:** [specs/beat-timing-audio/spec.md](../specs/beat-timing-audio/spec.md).

## Directive

Implement **reset to unknown BPM** (`CurrentBpm == 0`) for **`BpmSource.AudioAnalysis`** only:

1. **Stale beats:** If no beat is accepted for a defined wall-clock window (constant or central config — Lead default **4–8 seconds**; document final value in code + spec if it differs), set smoothed BPM and internal beat-interval state used for BPM so `CurrentBpm` reads **0**. Applies while buffers may still arrive (silence after music).
2. **Capture stopped:** When the app stops receiving audio from the active capture path (e.g. `StopCapture`, device teardown), clear audio-derived BPM to **0** so the header shows **`—`** without waiting for the stale window if that is simpler and still matches the spec scenario.

**In scope**

- `BeatDetector` and/or `AnalysisEngine` + `AudioDerivedBeatTimingSource` — minimal change set; prefer one clear owner for “tempo unknown.”
- `BeatTimingRouter` / source guards so Link and Demo are untouched.
- Unit tests under `tests/AudioAnalyzer.Tests/Application/BeatDetection/` proving stale window and/or capture path; use injectable time if `DateTime.Now` makes tests flaky.
- Update [specs/beat-timing-audio/spec.md](../specs/beat-timing-audio/spec.md) in the **same commit** if thresholds or triggers change during implementation.

**Out of scope**

- **`BeatCount`**, `_beatTimes` queue policy for Show, waveform strip beat marks — deferred to [PBI-010](PBI-010-reset-bpm-beatcount-beat-marks.md).
- New persisted `appsettings` keys — optional; if added, note in spec and consider a short ADR only if the decision is non-obvious.

## Context pointer

- Primary spec: [specs/beat-timing-audio/spec.md](../specs/beat-timing-audio/spec.md)
- Related ADR: [docs/adr/0066-bpm-source-and-ableton-link.md](../docs/adr/0066-bpm-source-and-ableton-link.md)

## Verification pointer

- Satisfy **Definition of Done**, **Regression guardrails**, and **Scenarios** in the spec.
- Commands: [AGENTS.md](../AGENTS.md).

## Refinement rule

If behavior or thresholds differ from the spec, **update the spec in the same commit**. Ambiguous product tradeoffs → flag for human review.
