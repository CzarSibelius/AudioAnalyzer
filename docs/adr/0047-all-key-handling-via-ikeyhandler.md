# ADR-0047: All key handling via IKeyHandler

**Status**: Accepted

## Context

Key handling is split between components that use `IKeyHandler<TContext>` (main loop, settings modal, TextLayers) and components that use inline methods (e.g. ShowEditModal.HandleKey, DeviceSelectionModal.HandleDeviceKey). Unifying on IKeyHandler improves consistency, testability, and dependency injection.

## Decision

1. **Universal contract**: Every component/class that handles keypresses must do so by implementing `IKeyHandler<TContext>` (with a context type appropriate to the component) or by delegating to an injected `IKeyHandler<TContext>`.

2. **No inline key logic**: Key-handling logic must not live in inline private methods or ad-hoc `Func<ConsoleKeyInfo, bool>` implementations; it belongs in a dedicated class that implements `IKeyHandler<TContext>`, registered in DI.

3. **Callers may forward**: Callers (e.g. modals, main loop) may still use a lambda that forwards to `handler.Handle(key, context)`; the requirement is that the actual logic lives in an IKeyHandler implementation.

4. **Adapter surfaces**: Existing adapter surfaces (e.g. `IVisualizationRenderer.HandleKey`, `IVisualizer.HandleKey`) may remain as pass-throughs that delegate to the visualizer's IKeyHandler where applicable (as TextLayers already does).

## Consequences

- Key handling has a single, consistent contract; handlers are discoverable and testable via DI.
- ShowEditModal and DeviceSelectionModal were migrated to use `IKeyHandler<ShowEditModalKeyContext>` and `IKeyHandler<DeviceSelectionKeyContext>` (handler classes in ShowEdit/ and KeyHandling/).
- References: [IKeyHandler](../../src/AudioAnalyzer.Application/Abstractions/IKeyHandler.cs), [ADR-0042](0042-ui-component-renderer-keyhandler-pattern.md).
