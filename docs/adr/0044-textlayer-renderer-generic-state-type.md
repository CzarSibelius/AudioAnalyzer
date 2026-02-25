# ADR-0044: Text layer renderer generic interface — state type in contract

**Status**: Accepted

## Context

Which per-layer state type a layer uses is not expressed in the renderer's type; it is only implied by constructor injection of `ITextLayerStateStore<TState>` and duplicated in TextLayersVisualizer's switch when clearing state on layer-type change. This makes it harder to discover, document, or evolve (e.g. reflection, tooling, or a single clearing mechanism) which state type belongs to which layer.

## Decision

1. **Abstract base class**  
   All text layer renderers inherit **`TextLayerRendererBase`**, which exposes `TextLayerType` and `Draw(...)`. This is the single type used by consumers for registration and collections (no non-generic interface).

2. **Marker interface**  
   **`ITextLayerRenderer<TState>`** is a marker interface with no members. It declares the per-layer state type in the type system. Layers implement **`TextLayerRendererBase`** and **`ITextLayerRenderer<TState>`** (e.g. `MarqueeLayer : TextLayerRendererBase, ITextLayerRenderer<NoLayerState>`).

3. **Stateless layers**  
   Layers that do not use per-layer animation state implement **`ITextLayerRenderer<NoLayerState>`**. A single shared sentinel type (`NoLayerState`) is used so that "no state" is explicit and consistent.

4. **Implementations**  
   - Stateful layers: inherit `TextLayerRendererBase` and implement `ITextLayerRenderer<TheirStateType>` (e.g. `BeatCirclesLayer : TextLayerRendererBase, ITextLayerRenderer<BeatCirclesState>`).  
   - Stateless layers: inherit `TextLayerRendererBase` and implement `ITextLayerRenderer<NoLayerState>` (e.g. `MarqueeLayer : TextLayerRendererBase, ITextLayerRenderer<NoLayerState>`).

5. **Registration**  
   Discovery and registration use the base class: register each implementation as `TextLayerRendererBase`. TextLayersVisualizer receives `IEnumerable<TextLayerRendererBase>` and builds a dictionary by `LayerType`.

## Consequences

- The state type for each layer is part of its public contract and discoverable via the type system (and reflection if needed).
- Stateless layers are explicit via `NoLayerState` instead of "no second interface."
- No non-generic interface is required; the abstract base class is the common type for DI and collections.
- New stateful layers (ADR-0043) must also implement `ITextLayerRenderer<NewState>`; new stateless layers implement `ITextLayerRenderer<NoLayerState>`.
- References: [ADR-0043](0043-textlayer-state-store.md), [ITextLayerStateStore](../../src/AudioAnalyzer.Visualizers/TextLayers/ITextLayerStateStore.cs), [TextLayerRendererBase](../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayerRendererBase.cs).
