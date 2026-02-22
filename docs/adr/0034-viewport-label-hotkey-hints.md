# ADR-0034: Viewport label hotkey hints

**Status**: Accepted

## Context

Labeled UI regions (header cells, toolbar cells) display "label: value" but do not indicate when the feature has a keyboard shortcut. Users must open the help menu to discover hotkeys. Some labels reference features that have dedicated keys (e.g. Preset with V, Device with D, Palette with P).

## Decision

1. **Format**: When a viewport label references a feature with a hotkey, include the hotkey in the label as **"Label(K):"** (e.g. "Preset(V):", "Device(D):"). When there is no hotkey, use **"Label:"**.

2. **Optional hotkey on APIs**: Labeled viewports and the components that render them accept an optional hotkey parameter:
   - `ScrollingTextViewport.FormatLabel(string label, string? hotkey)` — formats as "Label(K):" when hotkey is provided, else "Label:".
   - `ScrollingTextViewport.RenderWithLabel` gains optional `string? hotkey` parameter; when provided, formats label via `FormatLabel` before rendering.

3. **Manual builders**: When building label strings manually (e.g. toolbar cells in VisualizationPaneLayout), use `FormatLabel` so hotkeys appear in the label rather than after the value.

4. **Scope**: This applies to labeled UI regions (toolbar, header) that display "label: value". `VisualizerViewport` (bounds for visualizer output) is unchanged and does not take a hotkey property.

## Consequences

- Users see discoverable shortcuts directly in labels.
- `ScrollingTextViewport` gains `FormatLabel` and an extended `RenderWithLabel` overload.
- Call sites (ConsoleHeader, VisualizationPaneLayout) pass hotkeys where applicable; existing "(V)" or "(P)" suffixes after values are removed.
- References: [ScrollingTextViewport](../../src/AudioAnalyzer.Application/ScrollingTextViewport.cs), [ConsoleHeader](../../src/AudioAnalyzer.Console/Console/ConsoleHeader.cs), [VisualizationPaneLayout](../../src/AudioAnalyzer.Console/Console/VisualizationPaneLayout.cs).
