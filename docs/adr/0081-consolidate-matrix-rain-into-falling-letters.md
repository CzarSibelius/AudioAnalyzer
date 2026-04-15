# ADR-0081: Remove MatrixRain; extend FallingLetters (charset-only, column rain)

**Status**: Accepted

## Context

**MatrixRain** and **FallingLetters** both drew random glyphs from a character pool with overlapping UX (`CharsetId`, legacy `Custom.TextSnippets`, charset picker). Maintaining two layer types duplicated behavior and confused preset authors. FallingLetters already carried particle physics and richer beat reactions; MatrixRain’s value was mainly the **column-style** trail and **Flash** beat nudge.

## Decision

1. **Remove** `TextLayerType.MatrixRain`, `MatrixRainLayer`, `MatrixRainSettings`, and `MatrixRainBeatReaction` from the codebase.
2. **Extend FallingLetters** with:
   - **`FallingLettersAnimationMode`**: `Particles` (existing) and `ColumnRain` (port of the former MatrixRain column renderer; tail cells remain alternating `0`/`1` as before).
   - **`FallingLettersBeatReaction.Flash`** (new enum member at end): same discrete phase behavior as former MatrixRain Flash — column mode nudges draw phase per frame on beat; particle mode nudges spawn-phase for that frame only.
3. **Charset-only glyph pool for FallingLetters**: remove `TextSnippets` from `FallingLettersSettings`. Resolve glyphs with `CharsetResolver.ResolveByIdOrDefault(CharsetId, CharsetIds.Digits, literalFallback)`; when `CharsetId` is unset, **`digits`** is the default id (see `CharsetIds.Digits`).
4. **Runtime state**: when `AnimationMode` changes, clear stored particles for that layer slot (`FallingLettersLayerState`) so ColumnRain ↔ Particles does not leave stale particles.
5. **Presets**: JSON that references `"LayerType": "MatrixRain"` is **no longer valid** (string enum deserialization fails). Per [ADR-0029](0029-no-settings-migration.md), authors must edit presets manually or recreate from defaults; no automatic migration.

## Consequences

- S modal **charset list** no longer includes a **Legacy (TextSnippets)** row for glyph pools; `CharsetResolver.ResolveGlyphPool` was removed (FallingLetters uses `ResolveByIdOrDefault` only).
- Default preset ships **two** FallingLetters layers: one **Particles** + **SpawnMore**, one **ColumnRain** + **Flash** with explicit `CharsetId` **`digits`**.
- Specs and [ADR-0080](0080-shared-charset-json-and-layer-charset-ids.md) are updated: historical MatrixRain + snippet-pool wording is superseded for FallingLetters by this ADR.
