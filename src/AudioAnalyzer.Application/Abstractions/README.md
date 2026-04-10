# Application.Abstractions

This folder holds the **application-layer contracts** for AudioAnalyzer: interfaces and shared data types used by Console, Visualizers, Infrastructure, and Platform assemblies. Implementations live in those assemblies (or elsewhere in Application); nothing here should depend on concrete I/O or UI.

## Contents

- **Interfaces** — Service and component contracts for dependency injection (e.g. `IAudioInput`, `IVisualizer`, `ISettingsRepository`, `IDefaultTextLayersSettingsFactory`, `IKeyHandler<T>`, `IScrollingTextViewport`, `INowPlayingProvider`, `IAsciiVideoFrameSource`, `IAsciiVideoDeviceCatalog`, `ITitleBarNavigationContext`, `ITitleBarBreadcrumbFormatter`). See [ADR-0040](../../../docs/adr/0040-dependency-injection-preference.md), [ADR-0060](../../../docs/adr/0060-universal-title-breadcrumb.md), [ADR-0074](../../../docs/adr/0074-ascii-video-layer-and-frame-source.md).
- **DTOs / value types** — Cross-cutting data passed between layers: `AudioAnalysisSnapshot`, `VisualizationFrameContext`, `PresetInfo`, `NowPlayingInfo`, `AsciiVideoCaptureRequest`, `AsciiVideoFrameSnapshot`, `AsciiVideoDeviceEntry`, `VisualizerViewport`, `AudioDeviceEntry`, `ScrollingTextViewportState`, etc.

## Conventions

- New application-level interfaces that are injected or used across assemblies belong here.
- New shared data types (snapshots, info structs, event args) that cross assembly boundaries belong here.
- Concrete implementations and static utilities (e.g. ANSI helpers, parsers, buffers) belong in sibling folders under Application (e.g. `Display/`, `Viewports/`, `Palette/`) or in the implementing assembly (Console, Infrastructure, etc.).

## Related

- Console-specific UI contracts: [AudioAnalyzer.Console/Abstractions/](../../AudioAnalyzer.Console/Abstractions/) (sibling of Application).
- Domain types: [AudioAnalyzer.Domain](../../AudioAnalyzer.Domain/) (sibling of Application).