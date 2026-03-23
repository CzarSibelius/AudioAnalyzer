# ADR-0058: Per-layer render bounds and visual bounds editor

**Status**: Accepted

## Context

Users need to restrict individual text layers to a sub-rectangle of the visualizer viewport (e.g. fill only the bottom half, or a small overlay) so that outside that area the composite from lower z-order layers remains visible. A keyboard-driven, live (WYSIWYG) editor was requested so users can move and resize the region while seeing the effect.

## Decision

1. **Domain**: Add optional `TextLayerRenderBounds` on `TextLayerSettings` with normalized `X`, `Y`, `Width`, `Height` in **0–1** relative to the viewport width and height. `null` means the full viewport (default). Serialize as JSON under `RenderBounds`. No migration for missing property ([ADR-0029](0029-no-settings-migration.md)).

2. **Clipping**: `ViewportCellBuffer` implements a **clip stack**; `Set` respects the active clip; `Get` does not. Each frame the layered visualizer clears the clip stack, then for each enabled layer computes pixel bounds via `TextLayerRenderBounds.ToPixelRect`, `PushClip`, `Draw`, `PopClip`. This keeps `TextLayerDrawContext` unchanged ([ADR-0043](0043-textlayer-state-store.md)).

3. **Mirror layer**: `MirrorLayer` operates in **layer-local** pixel coordinates `(rx, ry, rw, rh)` from the same bounds (or full viewport when null). `MirrorSettings.SplitPercent` applies **within that rectangle**, not the whole screen.

4. **Visual edit**: `ITextLayerBoundsEditSession` (singleton) tracks the active sorted layer index. From the S modal, the **Render region** row (**Enter**) starts the session and closes the modal. **Arrows** move; **Shift+arrows** resize (bottom-right anchor). **Enter** commits; **Esc** restores the previous bounds. `MainContentContainer.HandleKey` dispatches to the session before the visualizer so **Esc** does not quit the app while editing. The visualizer draws a one-cell **box border** overlay on the composite before `FlushTo` while the session is active. Help adds a section when the session is active ([ADR-0049](0049-dynamic-help-screen.md)).

## Consequences

- All layers that only use `Set` automatically respect bounds; only Mirror required special handling because it reads the buffer.
- Presets remain resolution-independent via normalized coordinates.
- References: [TextLayerSettings](../../src/AudioAnalyzer.Domain/VisualizerSettings/TextLayerSettings.cs), [ViewportCellBuffer](../../src/AudioAnalyzer.Application/Viewports/ViewportCellBuffer.cs), [TextLayersVisualizer](../../src/AudioAnalyzer.Visualizers/TextLayers/TextLayersVisualizer.cs).
