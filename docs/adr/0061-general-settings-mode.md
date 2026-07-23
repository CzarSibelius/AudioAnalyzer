# ADR-0061: General Settings mode (ApplicationMode.Settings hub)

**Status**: Accepted

> **Update ([ADR-0093](0093-confirm-before-quit-and-deliberate-quit-keys.md)):** Decision item 5 below says Escape in the hub menu "falls through to the main loop (**quit**)". Per ADR-0093, top-level Escape no longer quits directly — it opens the **quit confirmation modal**. So hub-menu Escape now falls through to the quit **confirmation**, not an immediate quit. Hub inline-edit Escape (cancel the edit) is unchanged.

## Context

Users need a dedicated place to change **application-wide** settings (audio input, title bar display name, and eventually UI palette) without the Preset/Show visualization. [ADR-0060](0060-universal-title-breadcrumb.md) reserved `ApplicationMode.Settings` and the `app/settings` breadcrumb; the enum and formatter path existed before the hub UI.

## Decision

1. **Mode**: **General Settings** is a top-level **application mode** (`ApplicationMode.Settings`), persisted in `VisualizerSettings.ApplicationMode` with other mode fields (per [ADR-0029](0029-no-settings-migration.md)).

2. **Tab cycle**: **Tab** cycles **Preset editor → Show play (if at least one show has at least one entry) → General settings → Preset editor**. If no show is eligible for play, **Show play is skipped** (Preset → General settings → Preset).

3. **No layer visualization**: The main area shows a **keyboard-driven hub** (`GeneralSettingsHubAreaComponent` + `GeneralSettingsHubKeyHandlerConfig`) instead of `VisualizerAreaComponent`. **S** does not open the preset or Show modal in this mode. **F** does not toggle fullscreen (fullscreen is cleared when entering General settings). The **header** shows **only the title breadcrumb row** (no Device/Now/BPM volume rows) to maximize space for the hub; the **toolbar** row below still shows the hub hint and optional palette swatch. See [ADR-0062](0062-application-mode-classes.md).

4. **MVP settings**: **Audio input devices** (reuse `IDeviceSelectionModal` / same flow as **D**), **Application name** (`UiSettings.TitleBarAppName`, persisted via `IAppSettingsPersistence`), **Default asset folder** (`UiSettings.DefaultAssetFolderPath` — optional global base for AsciiImage / AsciiModel directory settings; when unset, layers use `AppContext.BaseDirectory`), and **UI theme** (`UiSettings.UiThemeId` → `themes/*.json`, [ADR-0071](0071-ui-themes-separate-from-palettes.md)). **Application UI palette** (`UiSettings.Palette` semantic slots) when **(Custom)** remains configurable via appsettings.

5. **Key routing**: `ApplicationShell` invokes `IKeyHandler<GeneralSettingsHubKeyContext>` when `ApplicationMode == Settings` **before** `IVisualizationRenderer.HandleKey` and the main-loop handler. **Escape** in the hub menu (not editing) falls through to the main loop, which opens the **quit confirmation** modal ([ADR-0093](0093-confirm-before-quit-and-deliberate-quit-keys.md)) rather than quitting immediately. **Escape** while editing inline text (application name or default asset folder) **cancels** the edit in the hub handler.

6. **Help**: Dynamic help ([ADR-0049](0049-dynamic-help-screen.md)) includes a **General settings hub** section when mode is Settings; **Layered text** shortcuts are omitted in that mode.

## Consequences

- New types: `GeneralSettingsHubState`, `GeneralSettingsHubEditMode`, `GeneralSettingsHubKeyContext`, `GeneralSettingsHubKeyHandlerConfig`, `GeneralSettingsHubAreaComponent`, `GeneralSettingsHubAreaRenderer`; main layout is owned by **`SettingsApplicationMode`** ([ADR-0062](0062-application-mode-classes.md)).
- README and UI spec document Tab and hub behavior.
- Supplements [ADR-0031](0031-show-preset-collection.md) (Tab previously only Preset ↔ Show) and [ADR-0060](0060-universal-title-breadcrumb.md) (hub home path).

## References

- [ApplicationShell](../../src/AudioAnalyzer.Console/ApplicationShell.cs) — mode switch, hub key routing
- [MainContentContainer](../../src/AudioAnalyzer.Console/Console/MainContentContainer.cs) — toolbar + hub vs visualizer
