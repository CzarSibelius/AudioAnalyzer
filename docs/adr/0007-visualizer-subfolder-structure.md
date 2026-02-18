# ADR-0007: Visualizer subfolder structure

**Status**: Accepted

## Context

Visualizer implementations were previously stored flat in `src/AudioAnalyzer.Visualizers/`. As the number of visualizers grows and some gain supporting types (e.g. `BeatCircle` for Geiss), a flat layout makes it harder to split visualizers into separate projects or keep related files together. A clear, consistent structure improves maintainability and enables future refactoring.

## Decision

Each visualizer has its own subfolder under `src/AudioAnalyzer.Visualizers/`. The folder name matches the visualizer (e.g. `Geiss`, `Oscilloscope`, `SpectrumBars`). All files belonging to a visualizer—including helper types like `BeatCircle` for Geiss—live in that visualizer’s folder.

- **Namespace**: Keep `AudioAnalyzer.Visualizers` for all visualizer types. Folder structure is for organization; the flat namespace preserves the public API and avoids changes to `VisualizationPaneLayout` and other consumers.
- **New visualizers**: Add a new subfolder with the same name pattern as the visualizer. Supporting types stay in the same subfolder.
- **Registration**: Visualizers remain registered in `VisualizationPaneLayout` via the dictionary; no reflection or assembly scanning is required.

## Consequences

- **Organization**: Each visualizer’s code is grouped; related files stay together and can be split into separate projects later if desired.
- **Discoverability**: The folder structure makes it clear which files belong to which visualizer.
- **No API impact**: Consumers continue to use `AudioAnalyzer.Visualizers` and types such as `GeissVisualizer`; no changes to using directives or references.
- **SDK-style projects**: Implicit globbing includes all `.cs` files in subfolders; no `.csproj` changes needed.

**Note (post-ADR-0014)**: The structure has evolved. All visual content now lives as layers under `TextLayers/<LayerName>/`. The only `IVisualizer` is TextLayersVisualizer. There are no standalone visualizer subfolders (Geiss, Oscilloscope, etc.).
