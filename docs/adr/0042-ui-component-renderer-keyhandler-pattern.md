# ADR-0042: UI component pattern — renderer plus IKeyHandler&lt;TContext&gt;

**Status**: Accepted

## Context

Several UI components (main loop key routing, settings modal, TextLayers visualizer) evolved with a similar split: a component-specific renderer (or drawer) for output and a component-specific key handler for input. Each key handler had a slightly different method name (TryHandle vs Handle) and its own context type, but the shape was the same: accept `ConsoleKeyInfo` and a context, return `bool`. Unifying these would make the pattern explicit and consistent for new components.

## Decision

1. **Unified key-handler contract**: All context-based key handlers implement `IKeyHandler<TContext>` from Application.Abstractions, with a single method: `bool Handle(ConsoleKeyInfo key, TContext context)`. Semantics: return `true` when the key was handled (caller-specific meaning: e.g. main loop “handled”, settings modal “close”, TextLayers “consumed”).

2. **Use the generic directly**: Components depend on `IKeyHandler<TContext>` for their context type (e.g. `IKeyHandler<MainLoopKeyContext>`, `IKeyHandler<SettingsModalKeyContext>`, `IKeyHandler<TextLayersKeyContext>`). No component-specific key-handler interfaces; the generic contract is sufficient for DI and readability.

3. **Renderers stay component-specific**: Renderer interfaces (e.g. `ISettingsModalRenderer`, `ITitleBarRenderer`, `IHeaderDrawer`, `IVisualizationRenderer`) have intentionally different method signatures (different state, dimensions, outputs). Do not introduce a shared renderer interface; the common pattern is the **pair**: a component-specific renderer plus an `IKeyHandler<TContext>` implementation.

4. **New UI components**: When adding a UI component that needs both rendering and key handling, use (a) a component-specific renderer (or drawer) interface and (b) `IKeyHandler<TContext>` for a component-specific context type. Register the handler as `IKeyHandler<YourContext>` in DI; the component holds both dependencies and delegates draw and key handling to them.

## Consequences

- Key handling has a single, consistent contract across main loop, modals, and visualizers.
- New components follow the same pattern without inventing new handler shapes.
- Renderer interfaces remain type-safe and explicit; no forced shared signature.
- References: [IKeyHandler](../../src/AudioAnalyzer.Application/Abstractions/IKeyHandler.cs), [ADR-0026](0026-console-ui-architecture.md), [ADR-0035](0035-modal-dependency-injection.md).
