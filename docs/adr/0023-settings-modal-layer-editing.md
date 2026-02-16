# ADR-0023: Settings modal for layer settings editing

**Status**: Accepted

## Context

Layer settings (common: Enabled, LayerType, ZOrder, BeatReaction, SpeedMultiplier, ColorIndex, PaletteId, TextSnippets; custom: Gain, LlamaStyle*, AsciiImage*, etc.) are edited in scattered ways: some in the S modal (type, enabled), some via global keys when a layer is selected ([ ] gain, P palette, I image), and others only via preset JSON. There is no single, documented place for layer editing.

## Decision

1. The **S settings modal** (opened with S) is the **canonical UI** for editing layer settings.

2. When adding or changing editing for layer properties (common or Custom), implement the editing in the S modal.

3. Quick-access keys (e.g. [ ] for Oscilloscope gain, P for palette) may remain as conveniences when a layer is selected outside the modal; the modal provides full editing.

4. The modal shows the selected layer's properties and allows editing them via keyboard (cycle, type, adjust). Layer-specific Custom settings are edited in the same place, branching on LayerType per [ADR-0021](0021-textlayer-settings-common-custom.md).

## Consequences

- New layer settings editing goes in `ShowTextLayersSettingsModal` (or shared modal logic), not in `TextLayersVisualizer.HandleKey` or other ad-hoc places.
- Layer *Settings.cs types and GetCustom/SetCustom remain the source of truth; the modal reads/writes via them.
- [ADR-0019](0019-preset-textlayers-configuration.md) and [ADR-0021](0021-textlayer-settings-common-custom.md) stay valid; this ADR complements them by specifying *where* editing lives.
- Visualizer specs and README should note that layer settings are edited in the S modal.
- **References**: [Program.cs ShowTextLayersSettingsModal](../../src/AudioAnalyzer.Console/Program.cs), [ADR-0006](0006-modal-system.md), [ADR-0019](0019-preset-textlayers-configuration.md), [ADR-0021](0021-textlayer-settings-common-custom.md).
