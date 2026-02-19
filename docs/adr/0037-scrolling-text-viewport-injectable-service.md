# ADR-0037: Scrolling text viewport as injectable service with data/render split

**Status**: Accepted

## Context

`ScrollingTextViewport` was a static class that rendered scrolling text (ping-pong) for dynamic UI content (device name, now-playing, settings hint) and provided label formatting. Consumers managed their own `ScrollingTextViewportState` and last-text for reset. This made testing harder and did not follow the injectable-component pattern used for `ITitleBarRenderer` (ADR-0036) and modals (ADR-0035). The logic also mixed scroll-state computation (data) with label/ANSI presentation (render).

## Decision

1. **Data part**: `IScrollingTextEngine` — stateless interface that advances scroll state and returns the visible slice. Implementation: `ScrollingTextEngine`. No ANSI; grapheme-cluster-aware via `IDisplayText.GetVisibleSubstring`.

2. **Render part**: `IScrollingTextViewport` — stateful viewport that holds its own scroll state and `_lastText` for auto-reset when content changes. Delegates scroll logic to `IScrollingTextEngine`; applies label formatting, ANSI colors, padding. Implementation: `ScrollingTextViewport`.

3. **Factory**: `IScrollingTextViewportFactory` — creates new viewport instances. One viewport per scroll region (e.g. device, now-playing, settings hint). Registered as singleton; `CreateViewport()` returns a new stateful viewport.

4. **State encapsulation**: Each viewport auto-resets when `text.Value != _lastText`, removing the need for callers to track `_deviceLastText` / `_nowPlayingLastText` and call `Reset()`.

5. **DI registration**: `IScrollingTextEngine` and `IScrollingTextViewportFactory` registered in ServiceConfiguration. `ApplicationShell` receives two viewports (device, now-playing) via factory; `SettingsModal` and `VisualizationPaneLayout` receive `IScrollingTextViewportFactory` and create viewports as needed.

6. **ConsoleHeader**: Receives `deviceViewport` and `nowPlayingViewport` as parameters from `ApplicationShell`; no longer owns static state.

7. **Static class removed**: The original `ScrollingTextViewport` static class is removed; all consumers use injectable components.

## Consequences

- Scrolling viewport is injectable and testable; alternative implementations can be registered.
- Consistent with ADR-0035 (modal DI) and ADR-0036 (title bar injectable).
- Clear separation: engine does scroll math; viewport does presentation and state.
- `ScrollingTextViewportState` remains; used internally by viewport and engine.
- References: [IScrollingTextEngine](../../src/AudioAnalyzer.Application/Abstractions/IScrollingTextEngine.cs), [ScrollingTextEngine](../../src/AudioAnalyzer.Application/ScrollingTextEngine.cs), [IScrollingTextViewport](../../src/AudioAnalyzer.Application/Abstractions/IScrollingTextViewport.cs), [ScrollingTextViewport](../../src/AudioAnalyzer.Application/ScrollingTextViewport.cs), [IScrollingTextViewportFactory](../../src/AudioAnalyzer.Application/Abstractions/IScrollingTextViewportFactory.cs), [ScrollingTextViewportFactory](../../src/AudioAnalyzer.Application/ScrollingTextViewportFactory.cs), [ConsoleHeader](../../src/AudioAnalyzer.Console/Console/ConsoleHeader.cs), [SettingsModal](../../src/AudioAnalyzer.Console/Console/SettingsModal.cs), [VisualizationPaneLayout](../../src/AudioAnalyzer.Console/Console/VisualizationPaneLayout.cs).
