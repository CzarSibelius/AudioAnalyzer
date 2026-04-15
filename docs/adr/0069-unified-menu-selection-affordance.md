# ADR-0069: Unified menu selection affordance

**Status**: Accepted

## Context

Selectable rows across settings surfaces used different cues: the General hub used a `>` prefix with foreground-only emphasis; modals used `►` with a full background block; the S-modal settings column and palette picker omitted the arrow or only highlighted part of a row; the UI theme list mixed prefix placement relative to ANSI highlights. That made keyboard selection harder to scan and tied highlight colors to ad hoc choices instead of one theme-driven recipe.

## Decision

1. **One affordance** for vertical selectable lists and hub menu rows: a leading `**►`** when the row is selected and    `****` when it is not (three terminal columns each, same glyphs everywhere).
2. **One highlight recipe** for selected rows: **background** + **foreground** covering the **full row width** (including the arrow and padding to the target width). Colors come only from the effective UI theme: `**UiPalette.Background`** with fallback `**ConsoleColor.DarkBlue**` and `**UiPalette.Highlighted**` for the selection foreground, resolved via `**IUiThemeResolver.GetEffectiveUiPalette()**` (or equivalent `UiPalette` passed from that resolver in render context).
3. **Shared implementation** — `**MenuSelectionAffordance`** in the Console project (with `**SettingsSurfacesListDrawing**` / `**SettingsSurfacesPaletteDrawing**` per [ADR-0063](0063-uniform-settings-list-and-palette-drawing.md)) is the canonical place for prefix constants, `**GetSelectionColors**`, and helpers that pad to display width while keeping the selection background continuous.
4. **Palette-colored names** (beat/tick-driven swatch text) remain inside selected rows where they exist today; the selection background wraps the full padded row so the arrow and padding match other lists.

## Consequences

- New selectable UI must use this affordance; update [ui-spec-menu-selection.md](../ui-spec-menu-selection.md) when adding surfaces.
- Hub rows lose per-grapheme value coloring **while the row is selected** (solid selection foreground on the selection background) so the block highlight stays visually consistent.
- `.cursor/rules/adr.mdc` references this ADR for agents.