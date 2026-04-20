# ADR-0082: Starfield text layer (pseudo-3D)

**Status**: Accepted

## Context

Users want a **classic pseudo-3D starfield** (“flying through stars”, similar in spirit to the Windows **Starfield Simulation** screensaver) as a composable **text layer** in `TextLayersVisualizer`, with enough **tuning knobs** for presets and live performance use, while respecting **ADR-0030** (no full-viewport per-pixel loops for this effect) and existing patterns for **layers**, **Custom JSON settings**, **charset-backed glyphs** ([ADR-0080](0080-shared-charset-json-and-layer-charset-ids.md)), **delta-time motion** ([ADR-0072](0072-delta-time-display-animation.md)), and **per-slot animation state** ([ADR-0043](0043-textlayer-state-store.md)).

## Decision

1. **Layer type**: Add `TextLayerType.Starfield` with a renderer `StarfieldLayer` (`TextLayerRendererBase`, `ITextLayerRenderer<StarfieldLayerState>`), auto-registered via existing `AddTextLayerRenderers()` reflection.

2. **Runtime state**: `StarfieldLayerState` lives in `TextLayerStateStore` as `ITextLayerStateStore<StarfieldLayerState>` — stars, optional sort scratch, accumulated center drift, tumble phase, RNG for fixed-seed mode, and last-known dimensions/count/seed for invalidation (resize / preset edits).

3. **Projection model** (normative for this layer):
   - Each star has model coordinates `(X, Y, Z)` with `Z` in `(ZNear, ZFar]` where `0 < ZNear < ZFar`. The camera looks from the origin along **+Z**; stars **move toward the camera** by decreasing `Z` each frame by an amount proportional to `BaseSpeed`, `SpeedMultiplier`, optional beat burst, and `ScaleForReference60(FrameDeltaSeconds)`.
   - **Perspective**: screen offsets from the view center `(cx, cy)` are  
     `screenX = cx + focalLength * X / Z`,  
     `screenY = cy + focalLength * Y / (Z * cellAspect)`  
     where **`cellAspect`** defaults to **2.0** so circular distributions read more round in tall console cells (same motivation as elliptical distance in BeatCircles).
   - **Spawn / respawn**: new stars get **uniform random Z** in `(ZNear, ZFar]` (not all at the far plane), so the field mixes near and far points for continuous “tunnel” travel. When `Z` falls at or below `ZNear`, respawn the same way with new `X`, `Y`, `Z`, and glyph index.
   - **Travel rate**: depth advance uses `(ZFar - ZNear) / TravelSeconds × BaseSpeed × … × frameDeltaSeconds` (with `TravelSeconds` clamped in code), so default motion crosses the depth range in tens of seconds instead of a few frames.

4. **Drawing order**: Sort active stars by **decreasing Z** (far first, near last) so nearer stars overwrite farther ones in the shared cell buffer.

5. **Performance**:
   - Work is **O(starCount + starCount log starCount)** for the sort; **no** iteration over every viewport cell for this layer.
   - **`StarCount`** is clamped in code to a **hard maximum** (1000) even if JSON requests more ([ADR-0030](0030-performance-priority.md)).

6. **Glyphs**: One character per star, chosen from a charset string resolved via `CharsetResolver.ResolveByIdOrDefault(..., CharsetIds.DensitySoft, literalFallback)` so presets work without shipping a dedicated charset file.

7. **Optional fixed RNG**: When `FixedRandomSeed >= 0`, spawns use `Random(FixedRandomSeed)` stored on layer state; when `FixedRandomSeed < 0`, use `Random.Shared` for spawns. On **full field reinit** (viewport resize, star-count change, or seed change), `StarfieldLayerState.RecreateFixedRandom` rebuilds the seeded `Random` so the **first** spawn sequence after reinit matches a cold start with the same seed (used by tests and reproducible presets).

8. **Settings migration**: None — new `Custom` shape only; incompatible edits follow [ADR-0029](0029-no-settings-migration.md).

## Consequences

- **Sort cost**: Large `StarCount` increases CPU; the hard cap and S-modal `[SettingRange]` on `StarCount` keep worst case bounded.
- **Out of scope for v1** (may be future work): long **motion streaks** per star, motion blur, stereo parallax.
- **Documentation**: Layer behavior and settings live in `docs/visualizers/starfield.md`; preset `Custom` example in `docs/configuration-reference.md`.
