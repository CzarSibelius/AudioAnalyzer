# ADR-0074: ASCII video layer and pluggable frame source (webcam first)

**Status**: Accepted

## Context

We want a text layer that renders **live video** as ASCII in the terminal. The first source should be a **webcam**; later we may support **video files**, streams, or other producers without redesigning the layer.

Today, new visual content is implemented as **text layer renderers** ([ADR-0014](0014-visualizers-as-layers.md)); layers that need data from outside the audio pipeline use **constructor-injected services**, not `AudioAnalysisSnapshot` or `VisualizationFrameContext` ([ADR-0028](0028-layer-dependency-injection.md), [ADR-0024](0024-analysissnapshot-frame-context.md)). **ASCII-from-raster** work already exists for static images (`AsciiImageLayer`, ImageSharp in the Visualizers project).

Video capture is **sensitive** (privacy, OS permissions) and **CPU-heavy** relative to many layers. The main redraw path targets **high frame rates** on capable hosts ([ADR-0067](0067-60fps-target-and-render-fps-overlay.md)); capture and decode must not stall or queue without bound ([ADR-0030](0030-performance-priority.md)).

The codebase already splits **Windows-specific** integrations from Application abstractions (e.g. `INowPlayingProvider` in Application; `WindowsNowPlayingProvider` in `AudioAnalyzer.Platform.Windows`; null/stub for tests).

## Decision

### 1. Pluggable frame source abstraction

1. Introduce an **Application-level** abstraction (working name `**IAsciiVideoFrameSource`**) that exposes the **latest** decoded frame in a form suitable for ASCII conversion (dimensions + pixel buffer or a small immutable DTO). The contract may allow “no newer frame since last read” to avoid redundant work.
2. **First concrete implementation**: **webcam** on **Windows**, implemented in `**AudioAnalyzer.Platform.Windows`**, registered from the console host’s DI setup (same pattern as now-playing).
3. **Out of scope for the initial implementation** (same interface, future registrations): video **file** playback, network streams, synthetic test patterns. The ADR assumes those add **new implementations** of the same abstraction, not new layer types per source.
4. Provide a **null or no-op** implementation (e.g. in Infrastructure or tests) so CI and environments without a camera do not open hardware.

### 2. Layer type and settings

1. Add a `**TextLayerType`** value named for **video/ASCII**, not webcam-only (e.g. `**AsciiVideo`**), so presets and UI stay stable when `SourceKind` grows.
2. Layer-specific settings live in `*Settings.cs` next to the layer ([ADR-0021](0021-textlayer-settings-common-custom.md)), discovered via reflection ([ADR-0025](0025-reflection-based-layer-settings.md)). At minimum:
  - `**SourceKind**`: e.g. `Webcam` first; reserve values such as `File` for later.
  - **Webcam**: device **index** or stable **id** string (implementation-defined mapping).
  - Optional **capture resolution** cap (downscale before ASCII mapping).
  - Charset / contrast / palette options **aligned with existing ASCII layers** where practical (reuse patterns from `AsciiImage` where it reduces duplication).
3. The **layer renderer** (in Visualizers) implements **draw logic** and injects `**IAsciiVideoFrameSource`** (and `**ITextLayerStateStore<TState>**` if buffers, scroll, or caching need per-layer state per [ADR-0043](0043-textlayer-state-store.md)). It does **not** embed Windows APIs or camera SDKs.

### 3. Threading and frame freshness

1. **Capture and decode** run **off the main render thread** (dedicated thread, async callback, or platform event loop). A **producer** updates a **latest-frame slot**.
2. `**Draw`** reads only the **latest** frame, using a **short lock** or **double-buffer** swap so the render path does not block on capture.
3. If ASCII conversion is slower than the camera frame rate, **drop intermediate frames**; do **not** grow an unbounded queue.
4. Prefer **downscaling** before luminance-to-character mapping so work scales with terminal size and performance goals ([ADR-0067](0067-60fps-target-and-render-fps-overlay.md)), analogous in spirit to `AsciiImageLayer`’s internal oversampling, tuned for video.

### 4. Dependencies and package choice

1. Any **NuGet** or native dependency for webcam access must satisfy [ADR-0013](0013-secure-nuget-packages.md) (maintained, trustworthy packages).
2. **Selection criteria** for the concrete stack: active maintenance, fit for **Windows** targets used by the console app, licensing compatible with the repo, and acceptable native payload if any.
3. The **concrete library/API** (WinRT, Media Foundation, third-party wrapper, etc.) is an **implementation detail** inside `**AudioAnalyzer.Platform.Windows`** until a second OS needs sharing; then revisit whether the abstraction or a shared helper project should move.

### 5. Camera lifecycle

1. **Default**: **lazy start** when the layer is actually used (e.g. first draw for an active preset that includes `AsciiVideo` with `Webcam`), and **release** on application shutdown or when the layer is no longer active—exact policy may use **refcounting** if multiple layers could share one source later.
2. Document follow-up if **multiple simultaneous** `AsciiVideo` layers or **shared** camera access require stricter coordination.

## Consequences

- **Privacy / UX**: Webcam access is sensitive; rely on **OS permission** flows; avoid using the camera when no preset requires it. Product copy and settings UI should make the source obvious when implemented.
- **Testing**: Use a **fake** `IAsciiVideoFrameSource` (solid color, small bitmap) in unit/integration tests; **do not** require physical hardware in automated runs.
- **Documentation when implementing**: Add a **visualizer spec** under `docs/visualizers/` and update `**docs/configuration-reference.md`** if JSON or appsettings surface changes, per project documentation rules.
- **Agent rule**: `.cursor/rules/adr.mdc` includes a pointer to this ADR for ASCII video / frame-source work.

## References

- [ADR-0014](0014-visualizers-as-layers.md), [ADR-0021](0021-textlayer-settings-common-custom.md), [ADR-0024](0024-analysissnapshot-frame-context.md), [ADR-0025](0025-reflection-based-layer-settings.md), [ADR-0028](0028-layer-dependency-injection.md), [ADR-0030](0030-performance-priority.md), [ADR-0043](0043-textlayer-state-store.md), [ADR-0044](0044-textlayer-renderer-generic-state-type.md), [ADR-0067](0067-60fps-target-and-render-fps-overlay.md), [ADR-0013](0013-secure-nuget-packages.md)

