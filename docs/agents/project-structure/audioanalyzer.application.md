# AudioAnalyzer.Application — folder layout

**`Abstractions/`**: interfaces and small DTOs consumed by Console, Infrastructure, or Visualizers (e.g. repositories, viewport/toolbar abstractions).

**Feature folders** (keep new code in the closest match):

- `BeatDetection/` — BPM / beat logic
- `Display/` — title bar, scrolling text, palette formatting, theme mapping
- `Fft/` — FFT analysis
- `Palette/` — palette loading/resolution
- `Viewports/` — cell buffers, static/scrolling viewports
- `VolumeAnalysis/` — volume / level analysis

**Project root**: only for types that are clearly cross-cutting services at the application layer and do not belong in a single feature folder (e.g. `UiThemeResolver.cs`, `AnalysisEngine` if present at root).

## Rules

- New feature areas may add a new top-level folder under `Application/` when the area is distinct and will hold multiple types; otherwise extend an existing folder.
- Do not put console-specific UI types here; those belong in `AudioAnalyzer.Console`.
