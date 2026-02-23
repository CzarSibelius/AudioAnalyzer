# AudioAnalyzer.Application

Application logic and abstractions. Contains the analysis engine, FFT processing, and interfaces used by the rest of the solution.

**Contents**:
- **Abstractions/** — Interfaces and DTOs (contracts only). See [Abstractions/README.md](Abstractions/README.md).
- **Display/** — ANSI/terminal display utilities: `AnsiConsole`, `DisplayWidth`, `TextHelpers`, `PlainText`, `AnsiText`, `StaticTextViewport`.
- **Viewports/** — `ViewportCellBuffer` (compositing buffer for layered visualizer output).
- **Palette/** — `ColorPaletteParser` (palette definition parsing).
- **Application root** — `AnalysisEngine` (analysis-only: processes audio, exposes results via GetSnapshot() and properties; display/rendering is orchestrated by the Console layer), `FftHelper`, `ScrollingTextEngine`, `ScrollingTextViewport`, `ScrollingTextViewportFactory`, and concrete implementations of Abstractions interfaces.

**Dependencies**: Domain
