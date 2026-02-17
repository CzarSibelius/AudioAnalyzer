# ADR-0027: Now-playing song in header

**Status**: Accepted

## Context

The application header had an empty line (row 5) that could display useful information. Windows exposes the Global System Media Transport Controls (GSMTC) API, which allows applications to read the currently playing media from other apps (Spotify, VLC, browsers, etc.). Showing "Artist - Title" in the header provides context without requiring the user to switch applications.

The app is Windows-only for audio capture (NAudio, WASAPI); a Windows-only now-playing feature is acceptable initially. The design should allow future cross-platform implementations (e.g. MPRIS on Linux).

## Decision

1. **Interface**: Add `INowPlayingProvider` in Application.Abstractions with `string? GetNowPlayingText()` returning formatted text (e.g. "Artist - Title") or null when no session exists.

2. **Platform implementations**:
   - **Windows**: `WindowsNowPlayingProvider` in `AudioAnalyzer.Platform.Windows` (targeting `net10.0-windows10.0.19041.0`) uses GSMTC: `GlobalSystemMediaTransportControlsSessionManager`, `TryGetMediaPropertiesAsync` for Title/Artist, background polling and event subscriptions for updates.
   - **Fallback**: `NullNowPlayingProvider` in Infrastructure returns null.

3. **Display**: Row 5 of the header shows now-playing when non-null. Uses `ScrollingTextViewport` for long text (per ADR-0020). Styled with ANSI DarkCyan. Scroll state resets when the track changes.

4. **DI**: Register `INowPlayingProvider` in ServiceConfiguration: `WindowsNowPlayingProvider` on Windows (with `Start()` to begin polling), `NullNowPlayingProvider` otherwise.

5. **Lifecycle**: `WindowsNowPlayingProvider` implements `IDisposable`; ServiceProvider disposes it on shutdown via `using var provider` in Program.

## Consequences

- The Console project targets `net10.0-windows10.0.19041.0` to reference Platform.Windows.
- Future Linux support would add `LinuxNowPlayingProvider` (MPRIS/D-Bus) and register it when `OperatingSystem.IsLinux()`.
- GSMTC requires the `globalMediaControl` capability for some scenarios; if access is denied, an app manifest with that capability may be needed.
