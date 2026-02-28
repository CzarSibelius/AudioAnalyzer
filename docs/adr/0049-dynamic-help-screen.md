# ADR-0049: Dynamic help screen — content based on active screen/view

**Status**: Accepted

## Context

Help is currently static: [HelpModal](../../src/AudioAnalyzer.Console/Console/HelpModal.cs) hardcodes every key and description, which duplicates handler logic and can drift (see [ADR-0048](0048-key-handlers-expose-bindings.md)). ADR-0048 added `GetBindings()` on all key handlers for discovery and stated that the primary consumer would be a future dynamic help screen; we want the help screen to consume that and vary by active view. Users benefit from seeing the shortcuts that apply to the screen they are on first (e.g. in Show play: "S = Show edit" and show-edit bindings; in Preset editor: "S = Preset modal" and preset-modal bindings).

## Decision

1. **Help content is dynamic**: Assembled at show time from handler bindings (via `GetBindings()`) and optional short explanatory blurbs. No hardcoded key/description lists in HelpModal; key lists come from handlers.

2. **Active view / mode-specific content**: When help is opened from the main loop, "active view" is the current **application mode** (`ApplicationMode.PresetEditor` or `ApplicationMode.ShowPlay`). The help screen shows **only** keybindings relevant to that mode: section-level (e.g. only "Preset settings modal" in Preset editor, only "Show edit modal" in Show play) and binding-level (bindings with `KeyBinding.ApplicableMode` set are shown only when help is opened in that mode).

3. **Content source**: A single place (e.g. provider or registry) aggregates bindings from the relevant `IKeyHandler` implementations, grouped by section/view. HelpModal receives the current view (e.g. `ApplicationMode`) and uses that to build the displayed sections (order or subset). Implementation may extend `IHelpModal.Show` with a context parameter (e.g. current mode) or inject an `IHelpContentProvider` that takes context and returns sections/bindings.

4. **Sections**: Use existing `KeyBinding.Section` from handlers for grouping (e.g. "Keyboard controls", "Preset settings modal", "Show edit modal", "Device selection", "Layered text"). Optional short blurbs (e.g. "Preset editor: Each preset is a TextLayers config…") may remain as static text; only key/shortcut lists are from `GetBindings()`.

## Consequences

- HelpModal (or a dedicated help-content builder) gets access to current view and to bindings (injected provider or handlers). `IHelpModal.Show` may gain an optional context parameter (e.g. `ApplicationMode`) for view-aware content; if so, the caller (MainLoopKeyHandler) already has `ctx.GetApplicationMode()` and passes it when opening help.
- New key handlers that implement `GetBindings()` (per ADR-0048) automatically contribute to help once registered in the aggregation used by the help screen.
- ADR-0048's intended consumer (dynamic help) is realized.
- Index in [README.md](README.md) and agent rules (e.g. `.cursor/rules/adr.mdc`) are updated to reference this ADR.

## Implementation

- **IHelpContentProvider**: Aggregates bindings from all five key handler configs (MainLoop, DeviceSelection, SettingsModal, ShowEditModal, TextLayers), filters by current mode (bindings with `ApplicableMode` set are included only when they match the current mode), groups by `KeyBinding.Section`, and returns sections. Only the modal section for the current mode is included (Preset settings modal in Preset editor, Show edit modal in Show play). Section order: Keyboard controls → Layered text → that modal → Device selection → General.
- **HelpModal**: Injects `IHelpContentProvider` and `UiSettings`; `Show(ApplicationMode? currentMode, ...)` stores mode and renders sections from `GetSections(currentMode ?? PresetEditor)`, plus static "Presets & Shows" blurb and "Press any key to return". "Current: Preset editor" / "Current: Show play" line shown under the title.
- **HelpSection**: DTO in Console Abstractions (section title + list of `KeyBinding`). Help content is built at show time; no hardcoded key lists in HelpModal.
