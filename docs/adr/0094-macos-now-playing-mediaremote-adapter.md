# ADR-0094: macOS now-playing via MediaRemote adapter (Perl + helper framework)

**Status**: Accepted

## Context

[ADR-0027](0027-now-playing-header.md) defines the cross-platform now-playing contract — `INowPlayingProvider.GetNowPlaying()` returning `NowPlayingInfo` (Title, Artist, Album) — and a Windows implementation (`WindowsNowPlayingProvider`) backed by GSMTC. On macOS the app currently registers `NullNowPlayingProvider`, so the header `Now:` row and the `NowPlaying` text layer are always empty. We want the same "read what another app (Music, Spotify, a browser, …) is playing" behaviour on the macOS host build (`net10.0-macos26.0`, [ADR-0086](0086-macos-windows-hosts-and-screencapturekit.md)).

The obvious native equivalent of GSMTC is Apple's **private `MediaRemote.framework`** (`MRMediaRemoteGetNowPlayingInfo`). It is **not usable** on this target:

- Since **macOS 15.4**, the `mediaremoted` daemon enforces entitlement checks and denies any client that is not an Apple-entitled process. The required entitlements (`com.apple.mediaremote.now-playing-read-access`, `…full-now-playing-read-access`) are **restricted** — granted by Apple at signing/provisioning time, not by a user-grantable TCC prompt. A self-signed / ad-hoc third-party app (which is how we sign the bundle per [ADR-0091](0091-macos-tap-explicit-consent-output-driven-aggregate-stable-signing.md)) cannot obtain them. There is still **no public read API** (Apple Feedback FB17228659 remains open).
- This is a different gate from Microphone / System Audio Recording / Camera ([ADR-0087](0087-macos-core-audio-tap-system-audio.md)/[0088](0088-macos-coreaudio-only-and-signed-app-bundle.md)/[0074](0074-ascii-video-layer-and-frame-source.md)), which are **user-grantable TCC consent** and therefore work with our self-signed identity. Now-playing read has no such consent path.

The **WWDC26 "Now Playing" framework** (Swift, macOS 26+) does **not** solve this: it is **publish-only** — it lets *our* app surface *its own* playback to system surfaces (Lock Screen, Control Center, CarPlay) and respond to remote commands. It cannot read another app's now-playing state, which is exactly what this feature needs. It is therefore rejected for this purpose.

The working community approach on macOS 15.4 / 26 is [`mediaremote-adapter`](https://github.com/ungive/mediaremote-adapter) (BSD-3-Clause): a `com.apple.`-identified **platform binary** — `/usr/bin/perl` — is still permitted to load `MediaRemote`, so the adapter has Perl dynamically load a small **helper framework** (`MediaRemoteAdapter.framework`) that calls `MRMediaRemote*` and prints now-playing JSON to stdout. Our app spawns the Perl process and reads stdout. This is the same "borrow a trusted platform binary" trick the lighter `osascript` route uses, but with full MediaRemote coverage and real-time streaming. This repo already ships native macOS artifacts (Core Audio tap and AVFoundation video shims) built with CMake/clang and bundled into the `.app` by `scripts/macos/pack-bundle.sh` ([ADR-0087](0087-macos-core-audio-tap-system-audio.md)/[0088](0088-macos-coreaudio-only-and-signed-app-bundle.md)/[0074](0074-ascii-video-layer-and-frame-source.md)), so the build/bundle pattern is established.

## Decision

1. **Keep the contract.** No change to `INowPlayingProvider` / `NowPlayingInfo` or to consumers (`HeaderContainerStateUpdater` `Now:` row, `NowPlayingLayer`). Add a macOS implementation only. macOS **can** populate `Album` (GSMTC cannot), enriching `ToDisplayString()` for free.

2. **`MacOsNowPlayingProvider` in `AudioAnalyzer.Platform.macOS/NowPlaying/`** mirrors the Windows model: a background process feeds a thread-safe cache; `GetNowPlaying()` is a cheap synchronous cache read; the type implements `IDisposable` and is started from the platform DI factory. This satisfies [ADR-0030](0030-performance-priority.md) — no blocking work on the render/header thread — consistent with [ADR-0027](0027-now-playing-header.md).

3. **Mechanism = `mediaremote-adapter`.** The provider spawns
   `/usr/bin/perl <mediaremote-adapter.pl> <MediaRemoteAdapter.framework> stream --no-diff --no-artwork`,
   parses each stdout line as JSON (`System.Text.Json`), and maps `title` / `artist` / `album` to `NowPlayingInfo`; an empty payload (no reporting player) yields `null`. `--no-artwork` avoids hundreds of KB of base64 per update (we render text only); `--no-diff` makes each line a complete state so no diff-merge state machine is needed. Per the adapter contract, stderr lines are non-fatal (log at debug); a **non-zero process exit is fatal** — log and do **not** re-spawn (degrade to no data). The helper framework is **bundled, not linked** and **not** P/Invoked, so `MacOsNativeLibraryResolver` is not involved.

4. **Availability and fallback.** `MacOsMediaRemoteAdapterAvailability` checks that `/usr/bin/perl`, the bundled `.pl`, and the framework are present (optionally running the adapter `test` command). When unavailable (not run from the finalized `.app`, artifact not built, adapter broken by an OS update), the macOS DI factory falls back to `NullNowPlayingProvider`. Honour the existing `nowPlayingOverride` seam for tests.

5. **Vendor + build + bundle.** Vendor the BSD-3 adapter sources under `native/mediaremote-adapter/` **pinned to a specific commit/tag** (its README warns of breaking changes across minor revisions). Build the universal (`x86_64`+`arm64`) `MediaRemoteAdapter.framework` with its CMake target, like the other native artifacts; builds and tests must succeed **without** it present. `scripts/macos/pack-bundle.sh` / `FinalizeMacOsAppBundle` copy `mediaremote-adapter.pl` + `MediaRemoteAdapter.framework` into the bundle (`Contents/Resources/mediaremote-adapter/`). The existing `codesign --force --deep` re-sign covers the nested framework; this does **not** break the mechanism because the `com.apple.` permission attaches to **`/usr/bin/perl`'s** identity, not the framework's signature. **No new Info.plist usage string** is needed (there is no TCC entitlement for now-playing read).

6. **Licensing.** `mediaremote-adapter` is BSD-3-Clause, compatible with GPL-3.0-only distribution per [ADR-0075](0075-nuget-license-compatibility.md). Retain its `LICENSE` and copyright with the vendored sources.

## Consequences

- The macOS `Now:` header row and `NowPlaying` layer are populated from real system media; macOS additionally provides `Album`. [ADR-0027](0027-now-playing-header.md) is no longer Windows-only; its cross-platform note is updated to reference this ADR.
- New dependencies/risks recorded here: (a) `/usr/bin/perl` is a deprecated bundled macOS runtime and may be removed in a future macOS — long-term fragility; (b) the `com.apple.` allow-check is an unintentional loophole Apple may close — the adapter's `test` command lets us detect breakage and degrade gracefully; (c) the adapter API may change across minor revisions — pin the vendored version.
- Spawning a child `perl` process per app run is acceptable for a single long-running visualizer; the process is torn down (SIGTERM) on provider `Dispose()` following shutdown ordering ([ADR-0018](0018-shutdown-lock-ordering.md)).
- Affected areas (implementing PBI): new `MacOsNowPlayingProvider` + process/parse/availability types under `Platform.macOS/NowPlaying/`; `MacOsPlatformServiceCollectionExtensions` registration; `native/mediaremote-adapter/` + `native/README.md`; `pack-bundle.sh` + `FinalizeMacOsAppBundle`; specs (`platform-macos`, `console-ui/toolbar`, text-layers hub NowPlaying section); operator docs.
- Logging follows [ADR-0076](0076-configurable-application-logging.md); no empty catch blocks.
