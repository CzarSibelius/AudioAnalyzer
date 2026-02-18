# ADR-0031: Show — preset collection for performance auto-cycling

**Status**: Accepted

## Context

Users need to chain presets for live performance with timed transitions. The current "Preset editor" mode requires manual cycling (V key) between presets. For performing music, users want to select a collection of presets that auto-advance based on configurable duration (wall-clock seconds or music beats). The main UI that cycles presets should be explicitly named "Preset editor" to distinguish it from the new performance mode.

## Decision

1. **Show model**: A Show is a named, ordered collection of (PresetId, Duration) entries. Each entry has:
   - `PresetId`: Reference to a preset (resolved via IPresetRepository).
   - `Duration`: Either `Seconds` (wall-clock) or `Beats` (at current detected BPM from AnalysisSnapshot). For Beats when BPM is 0, no fallback is needed — elapsed beats are compared directly to duration beats.

2. **Storage**: Shows stored as JSON files in `shows/*.json` (same pattern as presets and palettes). `IShowRepository` with `GetAll()`, `GetById()`, `Save()`, `Create()`, `Delete()`.

3. **Application modes**:
   - **Preset editor**: Current behavior — manual V cycling, S opens Settings modal (preset/layer editing). Toolbar: "Preset: {name} (V)".
   - **Show play**: Auto-advance through active Show's presets based on per-entry duration. S opens Show edit modal. Toolbar: "Show: {showName} | Preset: {presetName}". Tab switches back to Preset editor.
   - Mode switch: **Tab** cycles between Preset editor and Show play (only when at least one Show exists and has entries).

4. **Settings persistence**: `VisualizerSettings` gains `ApplicationMode`, `ActiveShowId`, `ActiveShowName`. Persisted in appsettings.json. Per [ADR-0029](0029-no-settings-migration.md), no migration for old files — defaults apply.

5. **Show edit modal**: S in Show play opens overlay modal. Edit Show: add/remove/reorder entries, set per-entry duration (Unit: Seconds/Beats via U key, Value via Enter), P to cycle preset for selected entry, R rename show, N new show. Esc closes.

6. **ShowPlaybackController**: Ticks each frame when in Show play. Tracks current entry index, elapsed time/beats. When duration exceeded, loads next preset via CopyFrom into TextLayers.

7. **Settings inheritance**: Deferred. Show is compositional only (ordered list of preset refs + duration). Future ADR may add Show-level defaults or override chain (Show → Preset → Layer).

## Consequences

- New `shows/` directory; `IShowRepository`, `FileShowRepository`.
- Domain: `Show`, `ShowEntry`, `DurationConfig`, `DurationUnit`, `ApplicationMode`.
- ApplicationShell: Tab key, mode-aware S and V handling, ShowPlaybackController tick.
- VisualizationPaneLayout: Mode-aware toolbar (Preset editor vs Show play).
- Help modal and README updated with Tab and Show controls.
- ADR index and agent rules updated.
