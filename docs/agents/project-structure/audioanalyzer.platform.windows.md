# AudioAnalyzer.Platform.Windows — folder layout

**`Audio/`**: WASAPI-backed **`WindowsAudioDeviceInfo`** / **`WindowsWaveInAudioInput`** ([ADR-0084](../../adr/0084-macos-multi-target-and-platform-audio.md)).

**`NowPlaying/`**: Windows-specific now-playing provider(s) (e.g. `WindowsNowPlayingProvider`).

**`AsciiVideo/`**: Windows webcam frame source for the ASCII video text layer (e.g. `WindowsAsciiVideoFrameSource` with `WindowsAsciiVideoFrameSource.Logging.cs` for `[LoggerMessage]` partials, `WindowsAsciiVideoDeviceCatalog` for S modal labels, WinRT interop helpers).

**`Hosting/`**: Windows implementations of cross-platform host abstractions ([ADR-0092](../../adr/0092-platform-behavior-via-abstractions-and-di-module.md)) — `WindowsConsoleScreenDumpContentProvider` (Win32 console buffer read), `WindowsConsoleBufferController`, `WindowsCapsLockState`, `WindowsHostContentLocator`, `WindowsStartupDiagnostics`.

**Project root**: `WindowsPlatformServiceCollectionExtensions.AddWindowsPlatform(...)` registers all of the above plus `WindowsAudioDeviceInfo` / `WindowsDefaultDeviceFallbackPolicy`. It is called from the console host's single OS switch (`PlatformSelection`).

This project is small; new Windows-only adapters should get a subfolder named after the concern (`NowPlaying/`, `Hosting/`, etc.) if multiple related types appear.

## Rules

- Keep non-Windows code out of this project.
- Prefer mirroring the abstraction namespace shape from Application (`INowPlayingProvider` → `NowPlaying/` here).
