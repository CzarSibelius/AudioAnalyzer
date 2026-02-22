# ADR-0020: UI text components — scrolling for dynamic, ellipsis for static

**Status**: Accepted

## Context

UI text often exceeds terminal width. The app has `ScrollingTextViewport` for scrolling (ping-pong) and `StaticTextViewport.TruncateToWidth` for hard truncation. Ellipsis truncation is provided by `StaticTextViewport.TruncateWithEllipsis`. When to use which approach needed clear guidance for developers and agents.

## Decision

1. **Dynamic text** → Prefer `ScrollingTextViewport`. Use when text changes frequently (toolbar, status lines) or content is user-driven and may overflow. Scrolling preserves full content while fitting the viewport. The toolbar help line and the settings modal (S) help line follow this pattern; both auto-scroll when the overlay redraws periodically.

2. **Static text** → Use an ellipsis truncation component when truncation is acceptable. Add `TruncateWithEllipsis` (or equivalent) that truncates to the viewport width and appends "…" when the text exceeds it. Use for titles, labels, and other seldom-changing text where scrolling would be unnecessary. `TruncateToWidth` remains for cases where raw cut is acceptable (e.g. borders, fixed-format lines where ellipsis would look wrong).

## Consequences

- Developers and agents use `ScrollingTextViewport` for dynamic overflow text instead of silent truncation.
- `StaticTextViewport.TruncateWithEllipsis` and `TruncateToWidth` provide ellipsis and raw truncation. Both accept `IDisplayText`; use `PlainText` for unformatted and `AnsiText` when text may contain ANSI (see [ADR-0032](0032-typed-display-text.md)). Truncation lives in `StaticTextViewport` to keep `VisualizerViewport` focused on bounds for visualizer output.
- **References**: [ScrollingTextViewport](../../src/AudioAnalyzer.Application/ScrollingTextViewport.cs), [StaticTextViewport](../../src/AudioAnalyzer.Application/Display/StaticTextViewport.cs), [VisualizerViewport](../../src/AudioAnalyzer.Application/Abstractions/VisualizerViewport.cs), [VisualizationPaneLayout](../../src/AudioAnalyzer.Console/Console/VisualizationPaneLayout.cs), [ADR-0032](0032-typed-display-text.md).
