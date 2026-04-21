# PBI-003: Fractal readability followups

**Transient work item** — close after merge or split into separate PBIs. **Pick one or more scopes** below; do not expand beyond chosen bullets without spec refresh.

## Directive (optional tracks)

### Track A — Dwell curve v2

If defaults-only tuning (`[PBI-002-fractal-readability-defaults.md](PBI-002-fractal-readability-defaults.md)`) is insufficient: extend **Strong** plateau breakpoints / `t` band in `[FractalZoomAnimation.cs](../src/AudioAnalyzer.Visualizers/TextLayers/FractalZoom/FractalZoomAnimation.cs)`, **or** add a new `[FractalZoomDwell](../src/AudioAnalyzer.Visualizers/TextLayers/FractalZoom/FractalZoomDwell.cs)` value with documented phase→scale mapping.

**In scope:** animation helper, settings surface if new enum, `[FractalZoomAnimationTests.cs](../tests/AudioAnalyzer.Tests/Visualizers/TextLayers/FractalZoom/FractalZoomAnimationTests.cs)` (or add), spec Contract + Blueprint.

**Out of scope:** Log scale defaults (defaults PBI), anchor catalog.

### Track B — Exterior / ramp visibility

If product wants **texture outside the set** or fewer **space-only** cells: specify mapping in `[specs/text-layers-visualizer/layers/fractal-zoom/spec.md](../specs/text-layers-visualizer/layers/fractal-zoom/spec.md)` first (e.g. floor on normalized `t`, or first glyph not space), then implement in `[FractalZoomLayer.cs](../src/AudioAnalyzer.Visualizers/TextLayers/FractalZoom/FractalZoomLayer.cs)` and default charset behavior as needed.

**In scope:** mapping + tests that lock intended normalization if measurable.

**Out of scope:** New fractal math, arbitrary-precision zoom.

### Track C — Anchor catalog quality

Bias or extend `[FractalZoomIllusoryReseed](../src/AudioAnalyzer.Visualizers/TextLayers/FractalZoom/FractalZoomIllusoryReseed.cs)` anchors toward regions that read at ASCII resolution across the default scale band; document catalog intent in spec.

**In scope:** anchor array + tests for distance/segment invariants already covered.

**Out of scope:** Continuous zoom path in \mathbb{C}.

## Context pointer

- Primary spec: `[specs/text-layers-visualizer/layers/fractal-zoom/spec.md](../specs/text-layers-visualizer/layers/fractal-zoom/spec.md)`
- Prerequisite: prefer completing `[PBI-002-fractal-readability-defaults.md](PBI-002-fractal-readability-defaults.md)` before Track A/B unless Lead reprioritizes.

## Verification pointer

- Per track: new or updated unit tests for animation / mapping / reseed invariants.
- Build / test / format: root `[AGENTS.md](../AGENTS.md)`.

## Refinement rule

Tracks **A/B/C are independent**; merge as separate commits or separate PRs if scope creeps. **Update the spec in the same commit** as behavior changes.