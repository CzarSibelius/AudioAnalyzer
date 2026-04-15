# ADR-0080: Shared charset JSON and layer charset references

**Status**: Accepted

## Context

Several text layers map brightness, density, or randomness to **individual characters** using hardcoded strings or charset files. Other layers (Marquee, WaveText) use `**Custom.TextSnippets`** as **whole phrases**. Users need reusable, file-backed character definitions—similar to palettes (`palettes/*.json`) and UI themes (`themes/*.json`)—with a clear UX in the S settings modal.

## Decision

### Charsets vs TextSnippets (normative)

- **Charsets** (`CharsetId` on layer custom settings, resolved via `charsets/*.json`): use when the layer **selects one character at a time** for rendering (luminance ramp, density steps, random glyph from a pool, etc.). The ordered sequence is a **symbol set or ramp**, not user-facing prose.
- `**Custom.TextSnippets`**: use when the layer needs a whole phrase (or a list of phrases) for rendering—marquee text, wave text, static text rotation, etc. Do not use `TextSnippets` as a charset pool for new features. FallingLetters uses `**CharsetId` only** for its glyph pool ([ADR-0081](0081-consolidate-matrix-rain-into-falling-letters.md)).

### Storage and schema

1. **Files**: Character sets live in `**charsets/*.json`** next to the executable (same discovery pattern as `palettes/` and `themes/`). **Id** = filename without extension.
2. **Schema** (`CharsetDefinition` in Domain): optional `**Name`** (display); required `**Characters`** — a single JSON string holding the **ordered** character sequence (including spaces when meaningful). Empty `Characters` is invalid; implementations may enforce a reasonable maximum length (e.g. 4096) when loading or saving.
3. **Repository**: `ICharsetRepository` in Application (list + `GetById` + `Save` + `Create` for user-authored files). Infrastructure provides `FileCharsetRepository` with in-memory caching of loaded definitions for hot paths.

### Layer contract

Layers that need a charset store `**string? CharsetId`** on their layer-specific `*Settings` type (Custom JSON per ADR-0021). Properties exposed in the S modal use `**[CharsetSetting]`** so reflection yields `**SettingEditMode.CharsetPicker**`. Semantics of null/empty `CharsetId`:

- **FallingLetters**: `CharsetId` unset → resolve using `**CharsetIds.Digits`** via `CharsetResolver.ResolveByIdOrDefault` ([ADR-0081](0081-consolidate-matrix-rain-into-falling-letters.md)).
- **AsciiImage, AsciiVideo, AsciiModel (legacy gradient), FractalZoom, GeissBackground, UnknownPleasures**: `CharsetId` unset → resolve using **built-in default ids** (`CharsetIds` constants) matching previous hardcoded ramps/density strings—**no** preset migration pass ([ADR-0029](0029-no-settings-migration.md)); defaults apply via deserialization / resolver.

### Resolution

`CharsetResolver` (Application) centralizes loading by id with fallback literals when a file is missing or invalid.

### UI

The S modal supports `**CharsetPicker`** focus: list entries are **repository charsets**. Navigation mirrors palette picking (↑/↓, +/-, Enter save, Esc discard). Title breadcrumb shows the same `/editor` affordance when the charset list is open ([ADR-0060](0060-universal-title-breadcrumb.md)) via `PresetSettingsCharsetPickerActive`.

### Persisting user charsets

- `**Create` / `Save`** on the repository write `charset-{n}.json` style ids (like themes) for tooling or future modal authoring.
- **No silent auto-export** of every distinct string on each app save; optional future **“collect charsets from preset”** flow may be added separately.

### AsciiModel shape mode

The generated **shape** character table remains separate from JSON charsets in v1; only **legacy gradient** mode uses `CharsetId` + ramp string.

## Consequences

- New shipped files under `charsets/` must be copied to output with the console project (same as palettes).
- Layer renderers that resolve charsets should inject `**CharsetResolver`** (or `ICharsetRepository` + resolver) per [ADR-0028](0028-layer-dependency-injection.md).
- Visualizer specs and `docs/configuration-reference.md` document the Charset vs TextSnippets rule for authors and users.
- Tests may substitute `ICharsetRepository` via `ServiceConfigurationOptions` where needed.

## Update (current)

**FallingLetters / MatrixRain (superseded detail):** The MatrixRain layer type and FallingLetters **snippet glyph pool** were removed in favor of a single **FallingLetters** layer with `**AnimationMode`** and charset-only pools; see [ADR-0081](0081-consolidate-matrix-rain-into-falling-letters.md). Historical bullets in this file that mention MatrixRain or **Legacy (TextSnippets)** for FallingLetters describe pre-0081 behavior only.