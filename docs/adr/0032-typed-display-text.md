# ADR-0032: Typed display text (PlainText vs AnsiText)

**Status**: Accepted

## Context

The codebase had method pairs that differed only by whether the string contained ANSI escape sequences: `Render` vs `RenderWithAnsi`, `RenderWithLabel` vs `RenderWithLabelWithAnsi`. Callers had to manually choose the correct overload. Passing ANSI-styled text to the plain overload would mis-measure width (counting escape sequences as characters). ADR-0020 noted that an ANSI-aware truncation variant might be needed.

## Decision

1. **Introduce typed display text**: Use `IDisplayText` (interface), `PlainText` (unformatted), and `AnsiText` (may contain ANSI). Implement as `readonly struct` for minimal allocation overhead per ADR-0030.

2. **Unify APIs**: Replace overload pairs with single generic methods:
   - `ScrollingTextViewport.Render<T>(T text, ...)` and `RenderWithLabel<T>(string label, T text, ...)` where `T : IDisplayText`
   - `StaticTextViewport.TruncateToWidth<T>(T text, maxWidth)` and `TruncateWithEllipsis<T>(T text, maxWidth)` where `T : IDisplayText`

3. **Call-site wrapping**: Callers wrap strings explicitly: `new PlainText(deviceName)`, `new AnsiText(styled)`. No implicit conversion from `string` to avoid accidentally treating ANSI as plain.

4. **Polymorphic behavior**: The type selects the correct length/substring/truncation logic. `PlainText` uses `string.Length`, `Substring`, `PadRight`. `AnsiText` delegates to `AnsiConsole.GetVisibleLength`, `PadToVisibleWidth`, `GetVisibleSubstring`.

## Consequences

- Compile-time safety: cannot accidentally pass ANSI text to logic that assumes plain.
- Single API surface instead of `*WithAnsi` overloads.
- ANSI-aware truncation (ellipsis and raw) is supported via `AnsiText` without additional methods.
- Call sites must wrap: `Render(new PlainText(line))` instead of `Render(line)`.
- **References**: [IDisplayText](../../src/AudioAnalyzer.Application/Abstractions/IDisplayText.cs), [PlainText](../../src/AudioAnalyzer.Application/Abstractions/PlainText.cs), [AnsiText](../../src/AudioAnalyzer.Application/Abstractions/AnsiText.cs), [ScrollingTextViewport](../../src/AudioAnalyzer.Application/Abstractions/ScrollingTextViewport.cs), [StaticTextViewport](../../src/AudioAnalyzer.Application/Abstractions/StaticTextViewport.cs).
