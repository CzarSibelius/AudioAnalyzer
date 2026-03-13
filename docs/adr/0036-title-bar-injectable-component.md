# ADR-0036: Title bar as injectable component

**Status**: Accepted

## Context

The header title was a centered static string from `UiSettings.Title`. Users wanted a hierarchical breadcrumb showing app name, mode, preset, and active layer (e.g. `aUdioNLZR/Show/gig_friday[0]: oscilloscope`) with cyberpunk/hacker aesthetic for both usability and visual appeal. The title bar should be an injectable component following the same DI pattern as modals (ADR-0035).

## Decision

1. **Title bar format**: Display `{appName}/{mode}/{preset}[{z}]: {layer}` as a path-like breadcrumb. App name from `UiSettings.TitleBarAppName` or derived from `Title`; mode, preset, and layer use Hackerize style via `TextHelpers.Hackerize` (first letter lowercase, second uppercase, whitespace→underscores). The preset and layer are separated by `[{z_index}]:` where `z_index` is the 0-based z-order index of the active layer (e.g. `0` for back, `8` for front); if the visualizer does not provide a z-index, `/` is used instead, e.g. `aUdioNLZR/pReset/gig_friday[0]: oScilloscope`.

2. **Title bar as generic row**: The title bar is rendered as a **LabeledRowComponent** with one viewport: label empty, value from **ITitleBarContentProvider.GetTitleBarContent()** (returns preformatted `IDisplayText`), and `PreformattedAnsi` true so the row renderer does not apply palette colors and uses truncate-with-ellipsis. `ITitleBarContentProvider` is in Console Abstractions; `TitleBarContentProvider` implements it (breadcrumb logic, palette, Hackerize). Registered in ServiceConfiguration; HeaderContainer injects it and builds the title row. No dedicated TitleBarComponent or TitleBarRenderer (see ADR-0052: single generic row component for all lines).

3. **Cyberpunk styling**: Default neon palette (cyan, magenta, green, yellow for segments; dim gray for separators). Each segment colored separately. Optional `UiSettings.TitleBarPalette` overrides built-in defaults.

4. **UiSettings extension**: `TitleBarAppName` (optional short/stylized name, e.g. "aUdioNLZR"); `TitleBarPalette` (optional custom colors for AppName, Mode, Preset, Layer, Separator; Frame is unused when the title bar is a single line without a box).

5. **IVisualizer extension**: Optional `GetActiveLayerDisplayName()` returns snake_case layer type for the active layer; optional `GetActiveLayerZIndex()` returns the 0-based z-order index (or -1 if not applicable). The title bar receives the single `IVisualizer` via constructor injection and calls these methods for the breadcrumb. TextLayersVisualizer implements both; the interface defaults are null and -1.

6. **Header integration**: The header (IHeaderContainer) builds a composite whose first child is a LabeledRowComponent for the title (one viewport, preformatted). The title bar is a **single line** (breadcrumb only; no frame or box).

## Consequences

- Title bar content is pluggable via ITitleBarContentProvider; alternative implementations can be registered via ServiceConfigurationOptions.
- Consistent with ADR-0035 (modal DI), ADR-0033 (UI settings), and ADR-0052 (generic row component for all lines).
- References: [ITitleBarContentProvider](../../src/AudioAnalyzer.Console/Abstractions/ITitleBarContentProvider.cs), [TitleBarContentProvider](../../src/AudioAnalyzer.Console/Console/TitleBarContentProvider.cs), [TextHelpers](../../src/AudioAnalyzer.Application/Display/TextHelpers.cs), [HeaderContainer](../../src/AudioAnalyzer.Console/Console/HeaderContainer.cs), [UiSettings](../../src/AudioAnalyzer.Domain/UiSettings.cs), [TitleBarPalette](../../src/AudioAnalyzer.Domain/TitleBarPalette.cs).
