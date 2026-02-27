# ADR-0046: Screen dump (ASCII screenshot)

**Status**: Accepted

## Context

Users and AI agents need a way to capture the current terminal screen as text for bug reports or to describe visual issues. The app renders to the console (header, toolbar, visualizer); there was no built-in way to "screenshot" that output for sharing or automation.

## Decision

- **Feature**: A screen-dump feature that writes the visible console content to a timestamped text file (ASCII screenshot). Default output is plain ASCII (ANSI escape sequences stripped) so pastes and AI chats stay readable.
- **Capture**: On Windows, capture is implemented by reading the console screen buffer via P/Invoke (`ReadConsoleOutputCharacterW`, `GetConsoleScreenBufferInfo`). This captures exactly what is on screen without changing the render path. On non-Windows or API failure, the service returns null (graceful degradation).
- **Interactive**: Ctrl+Shift+E triggers a dump (terminal-friendly; avoids Windows/terminal shortcuts such as Print Screen, Ctrl+Shift+D, or Ctrl+Shift+S which may pause output). The file is written to a `screen-dumps` directory next to the executable as `screen-{yyyyMMdd-HHmmss}.txt`.
- **Automation**: CLI options `--dump-after N` and `--dump-path <dir>` allow scripts or AI agents to run the app for N seconds, dump the screen, then exit. When `--dump-after` is used and no device was saved, the app uses Demo Mode so the device-selection modal is skipped.
- **Implementation**: Console project owns the feature. `IScreenDumpService` defines `DumpToFile(bool stripAnsi, string? directory)`; `ScreenDumpService` implements it. The main loop key handler calls the service on Ctrl+Shift+E; `ApplicationShell.Run` accepts optional dump-after parameters and starts a background task that dumps then quits.

## Consequences

- Screen dumps are additive: no changes to header, toolbar, or visualizer render path.
- New screen-dump behavior (e.g. ANSI-on request, different path) should go through `IScreenDumpService`; default remains plain ASCII.
- On unsupported environments, dump is a no-op (returns null); hotkey and CLI still run but no file is written.
- Dump directory must be writable; same pattern as presets/shows next to the executable.
