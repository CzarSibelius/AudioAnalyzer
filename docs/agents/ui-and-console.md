# UI and console

## UI specifications

When working on console UI (header, modals, main view, layout, or any on-screen content):

1. **Read the format**: Follow [docs/ui-spec-format.md](../ui-spec-format.md) for the structure of UI spec documents (screenshot block + line reference).
2. **Read UI components**: Use [docs/ui-components.md](../ui-components.md) to understand which component is responsible for each part of the screen.
3. **Read existing specs**: When changing an existing screen, read the corresponding UI spec in `docs/` so line numbers and descriptions stay accurate. **Application modes** (Preset editor, Show play, General settings): [ui-spec-application-modes.md](../ui-spec-application-modes.md). Settings menus and modals: [ui-spec-settings-surfaces.md](../ui-spec-settings-surfaces.md) (index), plus [ui-spec-general-settings-hub.md](../ui-spec-general-settings-hub.md), [ui-spec-device-selection-modal.md](../ui-spec-device-selection-modal.md), [ui-spec-preset-settings-modal.md](../ui-spec-preset-settings-modal.md).
4. **Update or create specs when you change layout**: Refresh the screenshot (from a screen dump per ADR-0046) and update the "Line reference" section so every line is listed and described. For new screens or modals, create a new UI spec file following the format.

Do not complete console UI changes that affect what appears on screen without updating the relevant UI spec.

## Architecture (ADRs)

- **Universal title breadcrumb**: Row 0 uses **ITitleBarBreadcrumbFormatter** and **ITitleBarNavigationContext** on the main header and all modals (ADR-0060). Spec: [ui-spec-title-breadcrumb.md](../ui-spec-title-breadcrumb.md).
- **UI container and component renderer**: UI is composed from **IUiComponent** and **IUiComponentRenderer**. Single-line rows use **HorizontalRowComponent** with **ScrollingTextComponent** children; set data each frame via **SetRowData** / **SetFromDescriptor**. See ADR-0051, ADR-0052, ADR-0053, ADR-0054, ADR-0056, ADR-0057.
- **Display text**: Use **PlainText** for unformatted strings, **AnsiText** when content may contain ANSI escape sequences. Use **IScrollingTextViewport** for dynamic text that may exceed width; use **TruncateWithEllipsis** for static text. Display width (terminal columns): use GetDisplayWidth, TruncateToWidth, etc. per ADR-0039.
- **Label hotkeys**: When a labeled viewport references a feature with a hotkey, show it as "Label(K):" (e.g. "Preset(V):"). Use **LabelFormatting.FormatLabel** or pass hotkey to **RenderWithLabel**. See ADR-0034 (API remains; call sites may pass null so labels show as "Label:" only).
- **Key handling**: Every component that handles keypresses must implement **IKeyHandler&lt;TContext&gt;** or delegate to one. No inline key-handling logic. Handlers must expose bindings via the interface method for dynamic help. See ADR-0047, ADR-0048, ADR-0049.
- **UI alignment**: Left-aligned; 8-character blocks; label format "Label:value" (colon after label, no space before value). See ADR-0050.

## User control requirements

- Every feature must be toggleable by the user in real time via keyboard shortcuts where appropriate.
- New features should have a dedicated key binding when they are primary actions.
- Help (H key) documents available commands; content is driven by the active screen and handler bindings (GetBindings()). When adding new key-handled features, ensure the handler exposes its bindings so help stays accurate.
- Settings are persisted automatically when changed (ADR-0001); no manual save key is required.

## Terminal output

- All output must scale dynamically with terminal size. Check `Console.WindowWidth` and `Console.WindowHeight` (or the viewport dimensions provided) before rendering.
- Account for margins/labels when calculating available space. Prevent line wraps by ensuring content fits within the viewport width. Update display when terminal is resized.
