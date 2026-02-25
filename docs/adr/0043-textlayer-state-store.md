# ADR-0043: Text layer state store

**Status**: Accepted

## Context

`TextLayerDrawContext` carried six required properties used by exactly one layer type each: `FallingLettersForLayer`, `AsciiImageStateForLayer`, `GeissBackgroundStateForLayer`, `BeatCirclesStateForLayer`, `UnknownPleasuresStateForLayer`, `MaschineStateForLayer`. TextLayersVisualizer owned the corresponding per-layer lists and passed the i-th state into the context when building it for each layer. This duplicated the pattern ADR-0028 addressed for service-derived data: data that only one consumer needs was passed through a shared pipeline, coupling the visualizer and the context to every stateful layer type and requiring a new required property for each new stateful layer.

## Decision

1. **Per-layer animation state is held in a singleton that implements the generic interface `ITextLayerStateStore<TState>`** for each state type. The store keeps **one state per layer slot** (one list; slot index = layer index). The type of the state at each slot matches whatever layer type is at that slot; when a slot is empty or the layer type changes, the next `GetState<T>(layerIndex)` creates and stores a new `T` instance. The store exposes the non-generic `ITextLayerStateStore` with `EnsureCapacity(int layerCount)` and `ClearState(int layerIndex)`, and the generic `ITextLayerStateStore<TState>` with `GetState(int layerIndex)` and `SetState(int layerIndex, TState state)` (and `where TState : new()`).

2. **TextLayersVisualizer injects all six typed stores** (e.g. `ITextLayerStateStore<BeatCirclesState>`); it calls `EnsureCapacity(sortedLayers.Count)` on any one at the start of each render and uses the appropriate store’s `SetState` in `ClearLayerStateWhenSwitching` when the user changes layer type. **Stateful layers inject only the store for their state type** (e.g. `ITextLayerStateStore<BeatCirclesState>`) and in `Draw()` call `_stateStore.GetState(ctx.LayerIndex)`. Each layer thus depends only on its own `TState` (interface segregation).

3. **`TextLayerDrawContext` carries only shared frame data**: buffer, snapshot, palette, dimensions, speed burst, and layer index. It no longer has any layer-specific state properties. New stateful layers extend the store (implement `ITextLayerStateStore<NewState>` on the concrete store, register it in DI) and inject that typed store; they do not extend the context.

4. **Registration**: `TextLayerStateStore` is registered as a singleton; the same instance is registered as `ITextLayerStateStore` (non-generic) and as each of the six `ITextLayerStateStore<TState>`. Registration happens before `AddTextLayerRenderers()`. The TextLayersVisualizer factory receives a single `ITextLayerStateStore`.

## Update (one state per slot)

The store was refactored to hold **one state per layer slot** (single list keyed by layer index) instead of six lists of N. The state at each slot is created on demand when a layer of that type is drawn (`GetState<T>` creates and stores a new `T` if the slot is empty or holds a different type). A non-generic `ITextLayerStateStore` was added with `EnsureCapacity(int)` and `ClearState(int layerIndex)`. TextLayersVisualizer now injects a single `ITextLayerStateStore` and calls `ClearState(layerIndex)` when the user changes the layer type at that slot (no per-type switch). Layers still inject `ITextLayerStateStore<TState>` for typed `GetState`/`SetState`.

## Consequences

- TextLayerDrawContext has fewer required properties; adding a new stateful layer does not require changing the context type.
- Stateful layers follow the same dependency-injection pattern as layers that need services (ADR-0028): they receive what they need via constructor injection. The generic interface ensures each layer depends only on its own state type (e.g. `ITextLayerStateStore<BeatCirclesState>`).
- TextLayersVisualizer injects one store (non-generic) for capacity and clear; layers inject the typed store for their state type. The store uses one list per slot; adding a new stateful layer does not add another list.
- References: [ITextLayerStateStore](../../src/AudioAnalyzer.Visualizers/TextLayers/ITextLayerStateStore.cs), [ITextLayerStateStoreOfT](../../src/AudioAnalyzer.Visualizers/TextLayers/ITextLayerStateStoreOfT.cs), [TextLayerStateStore](../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayerStateStore.cs), [ADR-0028](0028-layer-dependency-injection.md), [text-layers.md](../visualizers/text-layers.md).
