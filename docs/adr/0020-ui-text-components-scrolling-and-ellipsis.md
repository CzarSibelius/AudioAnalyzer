# ADR-0020: UI text components — scrolling for dynamic, ellipsis for static

**Status**: Accepted

## Context

UI text often exceeds terminal width. The app has `ScrollingTextViewport` for scrolling (ping-pong) and `VisualizerViewport.TruncateToWidth` for hard truncation. There is no ellipsis truncation. When to use which approach is undocumented. Developers and agents need clear guidance to choose the right component for each situation.

## Decision

1. **Dynamic text** → Prefer `ScrollingTextViewport`. Use when text changes frequently (toolbar, status lines) or content is user-driven and may overflow. Scrolling preserves full content while fitting the viewport. The toolbar help line and the settings modal (S) help line follow this pattern; both auto-scroll when the overlay redraws periodically.

2. **Static text** → Use an ellipsis truncation component when truncation is acceptable. Add `TruncateWithEllipsis` (or equivalent) that truncates to the viewport width and appends "…" when the text exceeds it. Use for titles, labels, and other seldom-changing text where scrolling would be unnecessary. `TruncateToWidth` remains for cases where raw cut is acceptable (e.g. borders, fixed-format lines where ellipsis would look wrong).

## Consequences

- Developers and agents use `ScrollingTextViewport` for dynamic overflow text instead of silent truncation.
- A new helper (e.g. in `VisualizerViewport` or `AnsiConsole`) provides ellipsis truncation for static text. Implementation details (method name, overload vs separate method) can be decided when adding the helper; the decision is that such a component should exist.
- May need an ANSI-aware variant if truncated text contains escape sequences.
- **References**: [ScrollingTextViewport](../../src/AudioAnalyzer.Application/Abstractions/ScrollingTextViewport.cs), [VisualizerViewport.TruncateToWidth](../../src/AudioAnalyzer.Application/Abstractions/VisualizerViewport.cs), [VisualizationPaneLayout](../../src/AudioAnalyzer.Infrastructure/VisualizationPaneLayout.cs), [Program.ShowTextLayersSettingsModal](../../src/AudioAnalyzer.Console/Program.cs) (settings modal help line).
