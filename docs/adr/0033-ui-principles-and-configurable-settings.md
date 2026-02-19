# ADR-0033: UI principles and configurable settings

**Status**: Accepted

## Context

UI components (header, toolbar, modals, layer numbers) use hardcoded colors, titles, and scrolling speeds. Users and developers need consistent, configurable UI styling and behavior. The app already has visualizer palettes (indexed color arrays) but UI needs semantic slots (normal, highlighted, dimmed, label) for different text roles.

## Decision

1. **Use existing components**: Prefer existing components (e.g. `ScrollingTextViewport`, `StaticTextViewport.TruncateWithEllipsis`) when possible. See [ADR-0020](0020-ui-text-components-scrolling-and-ellipsis.md).

2. **UI settings in appsettings**: UI has its own configurable settings stored in the same `appsettings.json` file (no separate file). Settings include app title, UI palette, and default scrolling speed.

3. **UI palette structure**: Semantic slots that may differ from visualizer palettes (named slots vs indexed array):
   - **Normal** — default UI text
   - **Highlighted** — active/selected UI elements (e.g. selected layer number, now-playing)
   - **Dimmed** — disabled or low-emphasis text
   - **Label** — labels and headers (e.g. "Device:", "Now:")
   - **Background** (optional) — for future use (modals, etc.)

4. **No separators between viewports**: Do not add separators (e.g. ` | ` or ` │ `) between UI viewports. The "label + value" styling with correct UI palette colors (Label, Normal, Highlighted, Dimmed) provides sufficient visual separation.

5. **Component behavior**:
   - `ScrollingTextViewport.RenderWithLabel`: label uses Label color, dynamic text uses Normal color when palette is provided. When a label references a feature with a hotkey, include it in the label as "Label(K):" (e.g. "Preset(V):") — see [ADR-0034](0034-viewport-label-hotkey-hints.md).
   - Layer numbers (1–9) in toolbar use Highlighted (active), Dimmed (disabled), Normal (inactive).
   - Default scrolling speed for all scrolling components comes from UI settings.

6. **Persistence**: `UiSettings` lives under app-level config; per [ADR-0010](0010-appsettings-visualizer-settings-separation.md) and [ADR-0029](0029-no-settings-migration.md), no migration — incompatible files get backup and reset.

## Consequences

- UI components receive `UiSettings` (or equivalent) and apply palette colors consistently.
- `ScrollingTextViewport` gains optional color parameters for label and text; callers pass null for backward compatibility.
- Domain: `UiSettings`, `UiPalette` types; Application: `IUiPaletteProvider` if needed for DI.
- References: [UiSettings](../../src/AudioAnalyzer.Domain/), [ConsoleHeader](../../src/AudioAnalyzer.Console/Console/ConsoleHeader.cs), [ScrollingTextViewport](../../src/AudioAnalyzer.Application/Abstractions/ScrollingTextViewport.cs).
