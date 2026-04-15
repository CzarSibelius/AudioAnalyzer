# ADR-0034: Viewport label hotkey hints

**Status**: Superseded. **Canonical:** [ADR-0033](0033-ui-principles-and-configurable-settings.md) (UI labels and principles), [ADR-0049](0049-dynamic-help-screen.md) (help and bindings). Key hints are **not** shown in viewport or row labels (avoid duplication with in-value hints and with the help modal). The help modal (**H**) is the place for key-binding discovery.

**Update (current codebase):** Optional hotkey support has been **removed** from APIs: `LabelFormatting.FormatLabel(string? label)` (only `"Label:"`), `IScrollingTextViewport` / `ScrollingTextViewport` (no hotkey on `RenderWithLabel`), and `LabeledValueDescriptor` / `ScrollingTextComponent` (no `Hotkey` property).

## Context (historical)

Labeled UI regions (header cells, toolbar cells) display `label:value`. An earlier idea was to embed shortcuts in labels as `Label(K):`.

## Decision (historical — superseded)

The original ADR required **"Label(K):"** when a feature had a hotkey and optional hotkey parameters on viewport APIs. That approach was superseded: UI labels use `**Label:`** only; bindings are documented in the dynamic help screen.

## Consequences (current)

- No hotkey parameters on label formatting or labeled row data types.
- References: [LabelFormatting.cs](../../src/AudioAnalyzer.Application/Display/LabelFormatting.cs), [ScrollingTextViewport.cs](../../src/AudioAnalyzer.Application/ScrollingTextViewport.cs), [LabeledValueDescriptor.cs](../../src/AudioAnalyzer.Application/Abstractions/LabeledValueDescriptor.cs), [ADR-0049](0049-dynamic-help-screen.md).