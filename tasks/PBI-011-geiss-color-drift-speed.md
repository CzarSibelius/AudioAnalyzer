# PBI-011: Geiss color drift — slower base + respect layer speed

**Transient work item** — close after merge. **The Spec** holds state; this file holds **delta**.

## Directive

GeissBackground’s **hue / palette cycling** feels too fast because `ColorPhase` advances at a **fixed** rate (`0.08 * dtScale`) and ignores `SpeedMultiplier` and `SpeedBurst`, while plasma `Phase` already respects those.

Apply **A + B**:

- **A — Slower base drift:** Lower the per-frame color phase increment so a full hue sweep at `SpeedMultiplier` 1.0 and no burst is **noticeably slower** than today (comfortable to watch on a typical terminal refresh).
- **B — Couple to layer speed:** Advance `ColorPhase` using the **same** `layer.SpeedMultiplier` and `ctx.SpeedBurst` factors as `Phase` (same direction: higher speed → faster color drift; speed burst → faster drift during burst).

**In scope:**

- `[src/AudioAnalyzer.Visualizers/TextLayers/GeissBackground/GeissBackgroundLayer.cs](../src/AudioAnalyzer.Visualizers/TextLayers/GeissBackground/GeissBackgroundLayer.cs)` — replace the fixed `ColorPhase` increment with a formula that includes `SpeedMultiplier`, `SpeedBurst`, `dtScale`, and a **tuned** base coefficient (Dev picks the constant; it must satisfy **A** at 1× / no burst).
- `[specs/text-layers-visualizer/spec.md](../specs/text-layers-visualizer/spec.md)` — under GeissBackground / implementation notes: one sentence stating that color drift (hue / palette index progression) scales with **layer speed** and is slower by default than legacy fixed-rate behavior.
- Tests: add or extend only if there is an existing deterministic hook for GeissBackground; otherwise manual verification is sufficient (no new test file required solely for a magic constant).

**Out of scope:** New `GeissBackgroundSettings` property (e.g. separate color drift slider), changes to beat flash / plasma formulas, palette resolution, or charset behavior.

## Context pointer

- Primary spec: `[specs/text-layers-visualizer/spec.md](../specs/text-layers-visualizer/spec.md)` (GeissBackground)
- Implementation: `GeissBackgroundLayer` (`ColorPhase`, `Phase`, `hue` from `ColorPhase` + position + plasma)
- Related ADRs: `[docs/adr/0014-visualizers-as-layers.md](../docs/adr/0014-visualizers-as-layers.md)` (layer-only change)

## Verification pointer

- **Manual:** Default preset with GeissBackground at `SpeedMultiplier` 1.0 — color field drifts **slower** than pre-change build; lowering layer speed in settings slows color drift; speed burst (if observable on that preset) accelerates color drift in line with plasma.
- **Regression:** Plasma motion, `BeatReaction` flash boost, palette vs `GetGeissColor` fallback unchanged except **rate** of hue progression.
- Build / test / format: root `[AGENTS.md](../AGENTS.md)`.

## Acceptance criteria

1. `ColorPhase` delta per frame is proportional to `SpeedMultiplier` and `SpeedBurst` (same sense as `Phase`).
2. At 1.0 / no burst, color drift is **slower** than the previous fixed `0.08 * dtScale` behavior.
3. Spec updated in the **same commit** as the code change (same-commit rule).

## Refinement rule

If tuning the constant still feels wrong across refresh rates: document the chosen constant rationale in a short code comment only; do not expand scope to per-user settings without a new PBI.
