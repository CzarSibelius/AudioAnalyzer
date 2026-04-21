# PBI-002: Fractal readability defaults

**Transient work item** — close after merge. **The Spec** holds state; this file holds **delta**.

## Directive

Reduce long stretches where **FractalZoom** reads as empty (mostly ramp low glyphs / exterior at very zoomed-out scales) and shorten the perceived **fast zoom** at phase extremes by tuning **defaults** and any **bundled presets** that use this layer.

**In scope:**

- `[specs/text-layers-visualizer/layers/fractal-zoom/spec.md](../specs/text-layers-visualizer/layers/fractal-zoom/spec.md)` — Blueprint: add a short product note that defaults should favor visible escape-time structure on the grid; adjust default-value documentation if settings change.
- `[src/AudioAnalyzer.Visualizers/TextLayers/FractalZoom/FractalZoomSettings.cs](../src/AudioAnalyzer.Visualizers/TextLayers/FractalZoom/FractalZoomSettings.cs)` — e.g. narrower `LogScaleMin`/`LogScaleMax` span (less extreme zoom-out), default `Dwell` = `Strong` if agreed.
- Preset JSON (or equivalent) under the repo that pins FractalZoom — align with new defaults or document overrides.
- Tests: extend or add focused tests only if behavior contracts change (e.g. default enum); keep existing `FractalZoom`* tests green.

**Out of scope:** New public settings knobs, new dwell enum values, ramp/gamma mapping changes, anchor catalog changes — use `[PBI-003-fractal-readability-followups.md](PBI-003-fractal-readability-followups.md)`.

## Context pointer

- Primary spec: `[specs/text-layers-visualizer/layers/fractal-zoom/spec.md](../specs/text-layers-visualizer/layers/fractal-zoom/spec.md)`
- Hub: `[specs/text-layers-visualizer/spec.md](../specs/text-layers-visualizer/spec.md)`
- Related ADRs: none unless a cross-cutting preset policy needs one (`[docs/adr/README.md](../docs/adr/README.md)`).

## Verification pointer

- Manual: default / common preset — layer shows **boundary-like** structure for a clear majority of a zoom cycle, not mostly blank.
- Contract: existing fractal scenarios + **Definition of Done** in the layer spec.
- Build / test / format: root `[AGENTS.md](../AGENTS.md)`.

## Refinement rule

If implementation discovers missing constraints: **update the spec in the same commit** (same-commit rule). Ambiguous product calls: escalate to Lead.