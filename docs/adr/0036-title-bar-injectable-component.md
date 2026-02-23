# ADR-0036: Title bar as injectable component

**Status**: Accepted

## Context

The header title was a centered static string from `UiSettings.Title`. Users wanted a hierarchical breadcrumb showing app name, mode, preset, and active layer (e.g. `aUdioNLZR/Show/gig_friday[0]: oscilloscope`) with cyberpunk/hacker aesthetic for both usability and visual appeal. The title bar should be an injectable component following the same DI pattern as modals (ADR-0035).

## Decision

1. **Title bar format**: Display `{appName}/{mode}/{preset}[{z}]: {layer}` as a path-like breadcrumb. App name from `UiSettings.TitleBarAppName` or derived from `Title`; mode, preset, and layer use Hackerize style via `TextHelpers.Hackerize` (first letter lowercase, second uppercase, whitespace→underscores). The preset and layer are separated by `[{z_index}]:` where `z_index` is the 0-based z-order index of the active layer (e.g. `0` for back, `8` for front); if the visualizer does not provide a z-index, `/` is used instead, e.g. `aUdioNLZR/pReset/gig_friday[0]: oScilloscope`.

2. **Injectable component**: `ITitleBarRenderer` interface in `Abstractions/`; `TitleBarRenderer` implementation in `Console/`. Registered in ServiceConfiguration; injected into ApplicationShell and passed to `ConsoleHeader.DrawMain` and `DrawHeaderOnly`.

3. **Cyberpunk styling**: Default neon palette (cyan, magenta, green, yellow for segments; dim gray for separators). Each segment colored separately. Optional `UiSettings.TitleBarPalette` overrides built-in defaults.

4. **UiSettings extension**: `TitleBarAppName` (optional short/stylized name, e.g. "aUdioNLZR"); `TitleBarPalette` (optional custom colors for AppName, Mode, Preset, Layer, Separator, Frame).

5. **IVisualizer extension**: Optional `GetActiveLayerDisplayName()` returns snake_case layer type for the active layer; optional `GetActiveLayerZIndex()` returns the 0-based z-order index (or -1 if not applicable). The title bar receives the single `IVisualizer` via constructor injection and calls these methods for the breadcrumb. TextLayersVisualizer implements both; the interface defaults are null and -1.

6. **ConsoleHeader integration**: `DrawMain` and `DrawHeaderOnly` accept `ITitleBarRenderer` and delegate lines 1–3 (title box) to it. The title bar renders the frame and content.

## Consequences

- Title bar is pluggable and testable; alternative implementations can be registered via ServiceConfigurationOptions.
- Consistent with ADR-0035 (modal DI) and ADR-0033 (UI settings).
- References: [ITitleBarRenderer](../../src/AudioAnalyzer.Console/Abstractions/ITitleBarRenderer.cs), [TitleBarRenderer](../../src/AudioAnalyzer.Console/Console/TitleBarRenderer.cs), [TextHelpers](../../src/AudioAnalyzer.Application/Display/TextHelpers.cs), [ConsoleHeader](../../src/AudioAnalyzer.Console/Console/ConsoleHeader.cs), [UiSettings](../../src/AudioAnalyzer.Domain/UiSettings.cs), [TitleBarPalette](../../src/AudioAnalyzer.Domain/TitleBarPalette.cs).
