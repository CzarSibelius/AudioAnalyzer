# Visualizers

## Viewport contract

When adding or changing visualizers:

1. **Respect the viewport**: `IVisualizer.Render` receives `VisualizerViewport` (StartRow, MaxLines, Width). Never write more than `viewport.MaxLines` lines and no line longer than `viewport.Width`. Use `StaticTextViewport.TruncateToWidth` for raw truncation; use `StaticTextViewport.TruncateWithEllipsis` for static text. Wrap strings in `PlainText` or `AnsiText` per ADR-0020, ADR-0032.

2. **Clamp line count**: Derive bar height, grid height, or other row counts from `viewport.MaxLines` minus any fixed lines (headers, footers, separators). Ensure the total number of lines written does not exceed `viewport.MaxLines`.

3. **Clamp line length**: For dynamic content (bars, waveform, grid), limit columns or bands so the built line length does not exceed `viewport.Width`, or truncate the final string before writing.

This prevents visualizers from scrolling the console or wrapping lines and corrupting the rest of the UI.

## New visualizer content

Create new visualizer content as **text layer renderers** in TextLayersVisualizer: inherit **TextLayerRendererBase**, implement **ITextLayerRenderer&lt;TState&gt;** (stateless layers use **NoLayerState**). Do not create new standalone **IVisualizer** modes. See ADR-0014, ADR-0044.

- **Layer settings**: TextLayerSettings has common props plus Custom (JSON). Layer-specific settings go in *Settings.cs next to the layer; use `GetCustom<TSettings>()` in Draw. New settings: add *Settings.cs, use [SettingRange]/[SettingChoices]/[Setting], register in LayerSettingsReflection.s_customSettingsRegistry. See ADR-0021, ADR-0025.
- **Layer state**: Stateful layers use **ITextLayerStateStore&lt;TState&gt;**; call `GetState(ctx.LayerIndex)` in Draw. Do not add new required properties to TextLayerDrawContext. See ADR-0043.
- **Layer settings editing**: The S settings modal is the canonical UI for editing layer settings. Implement layer-editing in the S modal, not in HandleKey or other ad-hoc places. See ADR-0023.
- **Max layers**: Use **TextLayersLimits.MaxLayerCount** (9) for layer count limits, padding, defaults, and key bindings 1–9; do not hardcode 9. See ADR-0045.

## Visualizer specs

When adding or changing a visualizer:

1. Read `docs/visualizers/README.md` for the list of visualizers and spec files.
2. If modifying an existing visualizer, read its spec file. If adding a new one, create a new spec file following the same format.
3. Follow the viewport rule above and ADRs (e.g. encapsulation, TextLayers cell buffer) as applicable.

Keep the spec in sync with the implementation.
