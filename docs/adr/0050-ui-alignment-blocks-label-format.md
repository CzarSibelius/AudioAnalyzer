# ADR-0050: UI alignment, 8-character blocks, and label format

**Status**: Accepted

## Context

The console UI needs consistent, predictable layout: alignment, fixed block sizing for components, and a single label format so all labeled regions behave the same. Without explicit rules, alignment (e.g. title bar centering), cell widths, and label punctuation can drift.

## Decision

1. **Left alignment:** UI is left-aligned when possible. Content is placed at column 0 (or at the start of its region); padding is applied to the right to fill width. The title bar is left-aligned (no centering): content + right padding only. The title row is a HorizontalRowComponent with one preformatted viewport; see [ScrollingTextComponentRenderer.cs](../../src/AudioAnalyzer.Application/ScrollingTextComponentRenderer.cs) and [HeaderContainer.cs](../../src/AudioAnalyzer.Console/Console/HeaderContainer.cs).

2. **8-character blocks:** UI elements are positioned so that each component uses space in **8-character (column) blocks**. Label components default to **8 columns for the label** and **8 columns for the value**; these widths can be overridden case by case when needed (e.g. longer labels or values). Sizing and layout calculations use multiples of 8 where practical (e.g. minimum cell widths, toolbar segments). **Multi-segment toolbar rows** (Preset/Show **header** rows 2–3 and **main** row 4): use **spread** layout when possible—each segment **starts** on an 8-column boundary, the first field is at column 0, the last is flush right, and interior fields are spaced toward the row center; cell widths sum to the row width ([`ToolbarSegmentSpreadWidths.cs`](../../src/AudioAnalyzer.Application/Display/ToolbarSegmentSpreadWidths.cs)). Very narrow rows fall back to **packed** trailing padding between fields ([`ToolbarSegmentPackedWidths.cs`](../../src/AudioAnalyzer.Application/Display/ToolbarSegmentPackedWidths.cs)). See [`HeaderContainer.cs`](../../src/AudioAnalyzer.Console/Console/HeaderContainer.cs), [`MainContentToolbarLayout.cs`](../../src/AudioAnalyzer.Console/MainContentToolbarLayout.cs), and [ui-spec-toolbar.md](../ui-spec-toolbar.md).

3. **Label format:** Labels use a colon immediately after the label text with **no space** before the value: `Label:value` (not `Label: value`). This applies to all labeled UI (header, toolbar, modals). [LabelFormatting.FormatLabel](../../src/AudioAnalyzer.Application/Display/LabelFormatting.cs) (used from [ScrollingTextViewport.cs](../../src/AudioAnalyzer.Application/ScrollingTextViewport.cs) and row renderers) produces `"Label:"` with no trailing space; manual label+value concatenation must also never insert a space after the colon.

## Consequences

- [HeaderContainer.cs](../../src/AudioAnalyzer.Console/Console/HeaderContainer.cs): Device/Now and BPM/Beat/Volume rows use **spread** layout ([`ToolbarSegmentSpreadWidths`](../../src/AudioAnalyzer.Application/Display/ToolbarSegmentSpreadWidths.cs)); when **Bpm source** is audio analysis, the Beat cell reserves width for `*BEAT*` ([`ToolbarBeatSegmentLayout`](../../src/AudioAnalyzer.Application/Display/ToolbarBeatSegmentLayout.cs)). Ensure no space after `BPM:`, `Beat:`, `Volume/dB:`.
- Title row (HorizontalRowComponent with preformatted viewport): Left-align breadcrumb (content + right pad only; no centering). See HeaderContainer and ScrollingTextComponentRenderer.
- [VisualizationPaneLayout.cs](../../src/AudioAnalyzer.Console/Console/VisualizationPaneLayout.cs): Toolbar palette and other labeled cells use 8-char block sizing for label/value where applicable.
- No change to `FormatLabel`/`RenderWithLabel` contract (already "Label:"); call sites and hand-built label strings must not add a space after the colon.
- UI specs and [ui-spec-format.md](../ui-spec-format.md) reference these rules so documented layouts stay consistent.
