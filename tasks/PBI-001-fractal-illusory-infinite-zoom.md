# PBI-001: Fractal illusory infinite zoom

**Transient work item** — close after merge. **The Spec** holds state; this file holds **delta**.

## Follow-up (product)

After illusory zoom is in place, **visible structure vs. empty exterior** tuning is tracked separately:

- [`PBI-002-fractal-readability-defaults.md`](PBI-002-fractal-readability-defaults.md) — default log span, dwell, presets.
- [`PBI-003-fractal-readability-followups.md`](PBI-003-fractal-readability-followups.md) — optional dwell v2, ramp/exterior mapping, anchor catalog.

## Directive

Implement **illusory infinite zoom** for `FractalZoom`: on each zoom **phase wrap** (while `ZoomPhase` is normalized past 1.0), advance per-slot **segment** state and **re-seed** the Mandelbrot view anchor and Julia-mode constant nudges via `FractalZoomIllusoryReseed` (deterministic catalog + formulas). Add optional `**IllusoryInfiniteZoom`** setting (default **on**); when **off**, preserve legacy fixed anchor (-0.75, 0.05) and zero Julia nudge.

**In scope:** [`specs/text-layers-visualizer/layers/fractal-zoom/spec.md`](../specs/text-layers-visualizer/layers/fractal-zoom/spec.md), [`src/AudioAnalyzer.Visualizers/TextLayers/FractalZoom/FractalZoomLayer.cs`](../src/AudioAnalyzer.Visualizers/TextLayers/FractalZoom/FractalZoomLayer.cs), [`FractalZoomState.cs`](../src/AudioAnalyzer.Visualizers/TextLayers/FractalZoom/FractalZoomState.cs), [`FractalZoomSettings.cs`](../src/AudioAnalyzer.Visualizers/TextLayers/FractalZoom/FractalZoomSettings.cs), new `FractalZoomIllusoryReseed.cs`, unit tests under `tests/AudioAnalyzer.Tests/Visualizers/TextLayers/FractalZoom/`.

**Out of scope:** Arbitrary-precision or perturbation deep zoom; raising `MaxIterations` or log-scale **product tuning** for readability (see [`PBI-002-fractal-readability-defaults.md`](PBI-002-fractal-readability-defaults.md)).

## Context pointer

- Primary spec: [`specs/text-layers-visualizer/layers/fractal-zoom/spec.md`](../specs/text-layers-visualizer/layers/fractal-zoom/spec.md)
- Related ADRs: none expected unless a shared cross-layer re-seed service appears ([`docs/adr/README.md`](../docs/adr/README.md)).

## Verification pointer

- Contract: **Illusory re-seed advances across wraps**, **Viewport is not exceeded**, existing **Layer draws when enabled** scenario.
- Build / test / format: root [`AGENTS.md`](../AGENTS.md).

## Refinement rule

If implementation discovers missing constraints: **update the spec in the same commit** (same-commit rule). Ambiguous product calls: escalate to Lead.
