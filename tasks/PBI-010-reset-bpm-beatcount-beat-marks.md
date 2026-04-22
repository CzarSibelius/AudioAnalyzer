# PBI-010: Reset beat ordinal / marks when audio tempo clears (optional)

**Transient work item** — close after merge. Depends on [PBI-009](PBI-009-reset-bpm-audio-stale-capture.md) unless executed together in one change (avoid duplicate work).

**Status:** Optional follow-up — **do not start** until product decides whether clearing “unknown BPM” should also reset **beat-driven** state.

## Directive

When audio-derived BPM resets to unknown (`CurrentBpm == 0`) per [specs/beat-timing-audio/spec.md](../specs/beat-timing-audio/spec.md), decide and implement **one** policy (document in spec):

**Option A — BPM only (baseline after PBI-009)**  
No change: `BeatCount` and stored beat marks continue; only tempo display clears.

**Option B — Full detector soft-reset on same triggers**  
On the same stale / capture-stop conditions as PBI-009, also clear or realign:

- `BeatDetector` beat ordinal / flash / energy history as needed for consistent “fresh start” when music resumes.
- Any **AnalysisEngine** beat mark ring / `_trackedBeatCountForMarks` sync so waveform layers and Show **beats** duration do not carry an arbitrary offset after silence.

**In scope**

- Types touched by beat marks: `AnalysisEngine` (e.g. `SyncBeatMarksWithTiming`, ring buffers), callers of `BeatCount` for Show/export if behavior changes.
- Visualizer contract tests if snapshot beat fields or marks change semantics.
- Extend [specs/beat-timing-audio/spec.md](../specs/beat-timing-audio/spec.md) with chosen option, new scenarios, and **Regression guardrails** for Show and waveform strip.

**Out of scope**

- Ableton Link / Demo beat grids unless explicitly unified (default: no).

## Context pointer

- Primary spec: [specs/beat-timing-audio/spec.md](../specs/beat-timing-audio/spec.md)
- Prerequisite delta: [PBI-009](PBI-009-reset-bpm-audio-stale-capture.md)
- Related ADR: [docs/adr/0066-bpm-source-and-ableton-link.md](../docs/adr/0066-bpm-source-and-ableton-link.md)

## Verification pointer

- Spec **Scenarios** extended for `BeatCount` / marks (e.g. after stale reset, next beat increments from 0 or from prior — **must match spec**).
- [AGENTS.md](../AGENTS.md) gates.

## Refinement rule

Choosing Option A vs B is a **product decision** — confirm in spec before coding; same-commit spec updates when implementation locks the choice.
