# UI spec: Toolbar (Preset editor and Show play)

**Scope:** In **Preset editor** and **Show play**, the **Toolbar** is one logical region made of **four horizontal rows** in the console (screen **lines 1–4** in mode specs such as [ui-spec-preset-editor-mode.md](ui-spec-preset-editor-mode.md)). Each row is a **`HorizontalRowComponent`** with **`LabeledValueDescriptor`** + **`ScrollingTextComponent`** children ([ADR-0056](adr/0056-scrolling-text-as-uicomponent.md), [ADR-0057](adr/0057-horizontal-row-unified-single-line-rows.md)). Rows with **multiple** fields use **spread layout** ([`ToolbarSegmentSpreadWidths`](../src/AudioAnalyzer.Application/Display/ToolbarSegmentSpreadWidths.cs)): segment **starts** align to **8-column** boundaries; the **first** field is at column 0, the **last** is flush right (cell widths sum to the terminal width), and **interior** fields are spaced toward the **center** of the row (for three fields: left / near-center / right). If spread constraints cannot be met in a very narrow terminal, layout falls back to compact **packed** widths ([`ToolbarSegmentPackedWidths`](../src/AudioAnalyzer.Application/Display/ToolbarSegmentPackedWidths.cs)). **`Label:value`** with no space after the colon ([ADR-0050](adr/0050-ui-alignment-blocks-label-format.md)). The **full line** may be right-padded to the terminal width ([`HorizontalRowComponentRenderer`](../src/AudioAnalyzer.Console/Console/HorizontalRowComponentRenderer.cs)).

**Beat row (audio BPM only):** the **Beat:** cell reserves display width for the worst-case value including `*BEAT*` so toggling the flash does not shift columns ([`ToolbarBeatSegmentLayout`](../src/AudioAnalyzer.Application/Display/ToolbarBeatSegmentLayout.cs), `HeaderContainer` + `HeaderStateData.ReserveBeatSegmentLayoutWidth`).

**General settings** mode shows only **line 1** (title breadcrumb); toolbar rows 2–4 are not shown ([ADR-0061](adr/0061-general-settings-mode.md)).

## Toolbar rows (screen line numbers in Preset / Show)

| Line | Source | Content |
|------|--------|---------|
| **1** | [`HeaderContainer`](../src/AudioAnalyzer.Console/Console/HeaderContainer.cs) title row | Universal title breadcrumb (`ITitleBarContentProvider`); one full-width preformatted cell ([ui-spec-title-breadcrumb.md](ui-spec-title-breadcrumb.md)). |
| **2** | `HeaderContainer` row 2 | **Device:** device name; **Now:** now-playing (scrolls when long). |
| **3** | `HeaderContainer` row 3 | **BPM**, **Beat**, **Volume/dB** (values depend on BPM source and audio; see mode specs). |
| **4** | [`MainContentContainer`](../src/AudioAnalyzer.Console/Console/MainContentContainer.cs) + [`MainContentToolbarLayout`](../src/AudioAnalyzer.Console/MainContentToolbarLayout.cs) + visualizer | **Preset editor:** Layers (1–9 digits), optional contextual fields, **Palette**. **Show play:** **Show**, **Entry**, optional contextual fields, **Palette** — see [TextLayersToolbarBuilder](../src/AudioAnalyzer.Application/TextLayersToolbarBuilder.cs). |

Mode-specific screenshots and per-line references remain in [ui-spec-preset-editor-mode.md](ui-spec-preset-editor-mode.md) and [ui-spec-show-play-mode.md](ui-spec-show-play-mode.md).
