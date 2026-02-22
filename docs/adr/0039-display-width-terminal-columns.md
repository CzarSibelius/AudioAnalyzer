# ADR-0039: Display width for terminal columns (emoji, wide characters)

**Status**: Accepted

## Context

Device names and other UI text can contain emoji (e.g. speaker icon) or wide characters (CJK, fullwidth). The codebase previously used grapheme count for width: each grapheme (including emoji) counted as 1. Terminals render emoji and many wide characters as 2 columns. This mismatch caused the device viewport and other header elements to change width when scrolled to the leftmost position with an emoji visible—the grapheme-based output exceeded the allocated column count when emoji appeared.

## Decision

1. **Display-width utility**: `DisplayWidth` provides `GetGraphemeWidth(string, index)` and `GetDisplayWidth(string)`. Wide characters (emoji, CJK, fullwidth per Unicode East Asian Width) return 2; narrow characters return 1.

2. **New IDisplayText methods**: `GetDisplayWidth()`, `GetDisplaySubstring(startCol, widthCols)`, `PadToDisplayWidth(widthCols)`. Implemented in PlainText, AnsiText; AnsiConsole has static equivalents that skip ANSI escape sequences.

3. **Scrolling and truncation use display width**: ScrollingTextEngine, ScrollingTextViewport.RenderWithLabel, ConsoleHeader, VisualizationPaneLayout, TitleBarRenderer, and StaticTextViewport.TruncateToWidth/TruncateWithEllipsis use display width (terminal columns) when fitting text. Scroll offset and slice width are in column units.

4. **Existing grapheme APIs preserved**: `GetVisibleLength`, `GetVisibleSubstring`, `PadToWidth` remain for grapheme-based operations where that is the intended semantic.

## Consequences

- Device viewport and header lines maintain consistent column width regardless of emoji position.
- Scrolling and truncation correctly account for double-width characters.
- References: [DisplayWidth](../../src/AudioAnalyzer.Application/Display/DisplayWidth.cs), [AnsiConsole](../../src/AudioAnalyzer.Application/Display/AnsiConsole.cs), [ScrollingTextEngine](../../src/AudioAnalyzer.Application/ScrollingTextEngine.cs).
