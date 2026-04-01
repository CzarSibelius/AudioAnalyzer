# AudioAnalyzer.Platform.Windows — folder layout

**`NowPlaying/`**: Windows-specific now-playing provider(s) (e.g. `WindowsNowPlayingProvider`).

This project is small; new Windows-only adapters should get a subfolder named after the concern (`NowPlaying/`, etc.) if multiple related types appear.

## Rules

- Keep non-Windows code out of this project.
- Prefer mirroring the abstraction namespace shape from Application (`INowPlayingProvider` → `NowPlaying/` here).
