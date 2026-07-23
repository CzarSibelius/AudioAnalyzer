# PBI-018: macOS now-playing via MediaRemote adapter

**Transient work item** — close after merge. **The Spec** (`specs/platform-macos/spec.md`, plus the now-playing references in `specs/console-ui/toolbar/spec.md` and `specs/text-layers-visualizer/spec.md`) holds **state**; this file holds **delta**.

## Directive

Implement now-playing on the macOS host build so the header `Now:` row and the `NowPlaying` text layer show real system media (today macOS registers `NullNowPlayingProvider`). Implement [ADR-0094](../docs/adr/0094-macos-now-playing-mediaremote-adapter.md):

1. **`MacOsNowPlayingProvider`** in `src/AudioAnalyzer.Platform.macOS/NowPlaying/`, implementing `INowPlayingProvider` + `IDisposable`, mirroring `WindowsNowPlayingProvider`'s background-cache model ([ADR-0027](../docs/adr/0027-now-playing-header.md), [ADR-0030](../docs/adr/0030-performance-priority.md)): a background reader thread feeds a lock-protected `NowPlayingInfo?` cache; `GetNowPlaying()` returns it synchronously; `Dispose()` SIGTERMs the child process and joins. Add a `Start()` invoked from the DI factory.
2. **Process + parse helpers** (one file per type per [ADR-0016](../docs/adr/0016-csharp-documentation-and-file-organization.md)): a process wrapper that spawns `/usr/bin/perl <mediaremote-adapter.pl> <MediaRemoteAdapter.framework> stream --no-diff --no-artwork`, streams stdout lines, treats stderr as non-fatal (debug log), and on **non-zero exit does not re-spawn**; a `System.Text.Json` payload DTO mapping `title`/`artist`/`album` → `NowPlayingInfo` (empty payload → `null`). Resolve bundled artifact paths via `IHostContentLocator` / `MacOsHostContentLocator` (`Contents/Resources/mediaremote-adapter/`). The framework is **bundled, not linked** and **not** P/Invoked (no `MacOsNativeLibraryResolver` entry).
3. **`MacOsMediaRemoteAdapterAvailability`** (mirror `MacOsCoreAudioTapAvailability`): verifies `/usr/bin/perl` + bundled `.pl` + framework exist (optionally runs the adapter `test` command). Used by the DI factory to decide adapter vs `NullNowPlayingProvider`.
4. **DI registration** in [`MacOsPlatformServiceCollectionExtensions`](../src/AudioAnalyzer.Platform.macOS/MacOsPlatformServiceCollectionExtensions.cs): replace the `NullNowPlayingProvider` line with a factory that honours `nowPlayingOverride` (tests), checks availability, constructs + `Start()`s `MacOsNowPlayingProvider`, else falls back to `NullNowPlayingProvider`. Same shape as the Windows registration.
5. **Vendor + build + bundle.** Add `native/mediaremote-adapter/` with the BSD-3 sources **pinned to a specific commit/tag** (keep its `LICENSE`/copyright), buildable via CMake to a universal (`x86_64`+`arm64`) `MediaRemoteAdapter.framework` like the other shims; build/tests must pass **without** it. Extend [`scripts/macos/pack-bundle.sh`](../scripts/macos/pack-bundle.sh) and the `FinalizeMacOsAppBundle` target in [`AudioAnalyzer.Console.csproj`](../src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj) to copy `mediaremote-adapter.pl` + `MediaRemoteAdapter.framework` into `Contents/Resources/mediaremote-adapter/`; the existing `--deep` re-sign covers the framework. **No** new Info.plist usage string (no TCC entitlement for now-playing read).
6. **Docs** ([documentation rule](../.cursor/rules/documentation.mdc)): `native/README.md` build section; `docs/agents/project-structure/audioanalyzer.platform.macos.md` (new `NowPlaying/` folder); `docs/getting-started.md` macOS now-playing note; mention BSD-3 in `docs/configuration-reference.md` license/dependency notes if dependency versions are listed there.

**In scope:** the macOS provider + process/parse/availability types, DI registration swap, `native/mediaremote-adapter/` vendor + build, `pack-bundle.sh` + `FinalizeMacOsAppBundle` bundling, doc updates, and the spec/ADR text already authored.

**Out of scope:** changing `INowPlayingProvider` / `NowPlayingInfo` or any consumer (`HeaderContainerStateUpdater`, `NowPlayingLayer`); the lighter `osascript` mechanisms; MediaRemote playback **control** (`send`/`seek`/etc.); artwork; a Windows behavior change; Linux.

## Context pointer

- Primary spec: [`specs/platform-macos/spec.md`](../specs/platform-macos/spec.md) (now-playing section + scenarios)
- Related specs: [`specs/console-ui/toolbar/spec.md`](../specs/console-ui/toolbar/spec.md) (`Now:` row), [`specs/text-layers-visualizer/spec.md`](../specs/text-layers-visualizer/spec.md) (NowPlaying layer)
- ADRs: [ADR-0094](../docs/adr/0094-macos-now-playing-mediaremote-adapter.md) (this decision), [ADR-0027](../docs/adr/0027-now-playing-header.md), [ADR-0030](../docs/adr/0030-performance-priority.md), [ADR-0075](../docs/adr/0075-nuget-license-compatibility.md), [ADR-0076](../docs/adr/0076-configurable-application-logging.md), [ADR-0084](../docs/adr/0084-macos-multi-target-and-platform-audio.md), [ADR-0088](../docs/adr/0088-macos-coreaudio-only-and-signed-app-bundle.md), [ADR-0092](../docs/adr/0092-platform-behavior-via-abstractions-and-di-module.md)

## Verification pointer

- Contract: **Definition of Done**, **Regression guardrails**, **Scenarios** in `specs/platform-macos/spec.md` (now-playing populated from another app; degrades to `Null` when adapter unavailable).
- Add **unit tests** for the pure logic (mirror production layout per [ADR-0064](../docs/adr/0064-test-project-mirrors-production-layout.md)): JSON payload → `NowPlayingInfo` mapping (empty → null, missing artist, album present), and availability/path resolution. The process interaction itself is not unit-tested (as Windows GSMTC is not); tests keep using `NullNowPlayingProvider` via the `nowPlayingOverride` seam.
- Build / test / format: root [`AGENTS.md`](../AGENTS.md) — `dotnet build` (0 warnings), tests, `dotnet format --verify-no-changes`. macOS host: pass the pinned `-f net10.0-macos26.0` TFM; verify a finalized bundle contains the adapter artifacts and that `GetNowPlaying()` returns live data when a player is active.

## Refinement rule

If implementation reveals a better invocation (e.g. diffing vs `--no-diff`, polling `get` vs `stream`), bundle location, or availability strategy than the spec/ADR describe, **update** [`specs/platform-macos/spec.md`](../specs/platform-macos/spec.md) (and [ADR-0094](../docs/adr/0094-macos-now-playing-mediaremote-adapter.md) if the decision itself changes) **in the same commit** (same-commit rule). If the change is product-level or ambiguous, stop and flag for human review.
