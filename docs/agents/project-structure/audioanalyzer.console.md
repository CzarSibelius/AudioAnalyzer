# AudioAnalyzer.Console — folder layout

The **executable** hosts DI, the main loop, application modes, and console UI. Namespace is `AudioAnalyzer.Console` regardless of subfolder.

**`Abstractions/`**: console-specific interfaces (modals, orchestrator, display state, mode transitions, screen dump, etc.).

**`Console/`**: shared **console UI implementation** — header, main content container, device/help/settings/show modals as UI components, component renderer/updaters, title bar row, visualizer area renderer. (The folder name means “console UI layer,” not the whole project.)

**`GeneralSettingsHub/`**: General Settings mode hub — state, key context/handler config, menu lines, area renderer, edit mode.

**`KeyHandling/`**: `IKeyHandler` implementations and key contexts for console flows (main loop, device selection, UI theme selection, etc.).

**`SettingsModal/`**: preset/layer settings modal — renderer, state, key handler/context, row builders.

**`ShowEdit/`**: show edit modal key handling.

**Project root**: composition and wiring — `Program`, `ApplicationShell`, `ServiceConfiguration`, application modes, orchestration, persistence glue, cross-cutting console services (`ScreenDumpService`, `LayerSettingsReflection`, toolbar/layout helpers, etc.).

**Content (non-C#)**:

- `appsettings.json`, `palettes/`, `presets/`, **`models/`** — bundled files copied to output (`CopyToOutputDirectory` in `.csproj`). Do not remove or relocate without updating the project file and any code that resolves paths.

## Rules

- Feature-specific console code (one modal or one mode surface): prefer a dedicated folder (`SettingsModal/`, `GeneralSettingsHub/`, `ShowEdit/`) over piling into `Console/` or root.
- New modals or hubs: follow ADR-0035 / ADR-0053 (DI, `IUiComponent`).
