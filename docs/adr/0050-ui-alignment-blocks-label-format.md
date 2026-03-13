# ADR-0050: UI alignment, 8-character blocks, and label format

**Status**: Accepted

## Context

The console UI needs consistent, predictable layout: alignment, fixed block sizing for components, and a single label format so all labeled regions behave the same. Without explicit rules, alignment (e.g. title bar centering), cell widths, and label punctuation can drift.

## Decision

1. **Left alignment:** UI is left-aligned when possible. Content is placed at column 0 (or at the start of its region); padding is applied to the right to fill width. The title bar is left-aligned (no centering): content + right padding only. The title row is a LabeledRowComponent with one preformatted viewport; see [LabeledRowRenderer.cs](../../src/AudioAnalyzer.Application/LabeledRowRenderer.cs) and [HeaderContainer.cs](../../src/AudioAnalyzer.Console/Console/HeaderContainer.cs).

2. **8-character blocks:** UI elements are positioned so that each component uses space in **8-character (column) blocks**. Label components default to **8 columns for the label** and **8 columns for the value**; these widths can be overridden case by case when needed (e.g. longer labels or values). Sizing and layout calculations use multiples of 8 where practical (e.g. minimum cell widths, toolbar segments).

3. **Label format:** Labels use a colon immediately after the label text with **no space** before the value: `Label:value` (not `Label: value`). This applies to all labeled UI (header, toolbar, modals). The existing `FormatLabel` in [ScrollingTextViewport.cs](../../src/AudioAnalyzer.Application/ScrollingTextViewport.cs) produces `"Label:"` or `"Label(K):"` with no trailing space; manual label+value concatenation (e.g. in [ConsoleHeader.cs](../../src/AudioAnalyzer.Console/Console/ConsoleHeader.cs) for BPM/Beat/Volume) must also never insert a space after the colon.

## Consequences

- [ConsoleHeader.cs](../../src/AudioAnalyzer.Console/Console/ConsoleHeader.cs): Use 8-based widths for device/now cells and for BPM/volume cells where possible; ensure no space after `BPM:`, `Beat:`, `Volume/dB:`.
- Title row (LabeledRowComponent with preformatted viewport): Left-align breadcrumb (content + right pad only; no centering). See HeaderContainer and LabeledRowRenderer.
- [VisualizationPaneLayout.cs](../../src/AudioAnalyzer.Console/Console/VisualizationPaneLayout.cs): Toolbar palette and other labeled cells use 8-char block sizing for label/value where applicable.
- No change to `FormatLabel`/`RenderWithLabel` contract (already "Label:"); call sites and hand-built label strings must not add a space after the colon.
- UI specs and [ui-spec-format.md](../ui-spec-format.md) reference these rules so documented layouts stay consistent.
