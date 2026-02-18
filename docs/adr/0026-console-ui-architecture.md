# ADR-0026: Console UI architecture — modular presentation layer

**Status**: Accepted

## Context

UI code was concentrated in `Program.cs` (~745 lines), mixing bootstrap, main loop, header drawing, three modals, key routing, preset/palette logic, and device lifecycle. Console-specific components (`VisualizationPaneLayout`, `ConsoleDisplayDimensions`) resided in Infrastructure. This made the codebase hard to navigate and change.

## Decision

1. **Modular presentation within Console project**: Extract UI into focused classes under `Console/`:
   - `ConsoleHeader` — header drawing (DrawMain, DrawHeaderOnly)
   - `ModalSystem` — RunModal, RunOverlayModal (ADR-0006)
   - `HelpModal`, `DeviceSelectionModal`, `SettingsModal` — modal content and input handling
   - `ApplicationShell` — main loop, key routing, device lifecycle, preset/palette actions

2. **Thin bootstrap**: `Program.cs` loads settings, builds DI, resolves initial device, creates `ApplicationShell`, and calls `Run()`. Target ~50 lines.

3. **Console-specific code in Console project**: `VisualizationPaneLayout` and `ConsoleDisplayDimensions` move from Infrastructure to Console. Infrastructure keeps only adapters (NAudio, file repositories, synthetic audio).

4. **Enums**: `SettingsModalFocus` and `SettingEditMode` live in `Console/Enums.cs` for use by settings modal and layer reflection.

## Consequences

- Presentation logic is split by responsibility; adding modals or key bindings is localized.
- ADR-0006 modal system and ADR-0023 settings modal behavior stay intact.
- Infrastructure no longer depends on `System.Console`; Console project owns all console I/O.
- Application does not perform console I/O: `ViewportCellBuffer` flushes via `IConsoleWriter`, which is implemented by `ConsoleWriter` in the Console project. `TextLayersVisualizer` receives `IConsoleWriter` via DI and passes it to `ViewportCellBuffer.FlushTo`.
- References: [ApplicationShell](../../src/AudioAnalyzer.Console/ApplicationShell.cs), [Console/](../../src/AudioAnalyzer.Console/Console/), [ConsoleWriter](../../src/AudioAnalyzer.Console/ConsoleWriter.cs), [Program.cs](../../src/AudioAnalyzer.Console/Program.cs).
