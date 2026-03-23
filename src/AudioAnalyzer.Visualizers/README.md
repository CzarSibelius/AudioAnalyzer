# AudioAnalyzer.Visualizers

Visualization implementations. TextLayersVisualizer is the only `IVisualizer`. All visual content is implemented as text layer renderers (classes inheriting TextLayerRendererBase: Marquee, BeatCircles, Oscilloscope, GeissBackground, VuMeter, LlamaStyle, etc.).

**Contents**: `IVisualizer` implementation (TextLayersVisualizer), text layer renderers (TextLayerRendererBase: Marquee, ScrollingColors, BeatCircles, Oscilloscope, GeissBackground, UnknownPleasures, VuMeter, LlamaStyle, etc.), layer `*Settings` types, `DefaultTextLayersSettingsFactory` (`IDefaultTextLayersSettingsFactory` for default presets and padding layers)

**Dependencies**: Application, Domain
