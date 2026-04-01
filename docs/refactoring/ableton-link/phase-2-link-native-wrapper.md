# Phase 2: Link library and managed wrapper (standalone)

**Outcome:** A **native shim DLL** (or agreed alternative) wraps [Ableton Link](https://ableton.github.io/link/) and exposes a **minimal C ABI**. Managed code can **load**, **enable**, **read session tempo**, **peer count**, and **beat-at-time** (or equivalent) for future Phase 3 — **without** yet changing app BPM source behavior.

**Prerequisite:** Phase 1 complete (optional but recommended so Phase 3 only adds `LinkBeatTimingSource`).

## Repository layout and license

- [x] Add git **submodule** for `https://github.com/Ableton/link` under e.g. `native/ableton-link/link/` (exact path documented in README).
- [x] Document **GPL-2.0+** obligations in `README.md` and in ADR from Phase 1/3 (source offer, distribution).
- [x] Choose integration approach (default: **C shim + P/Invoke**; alternatives: C++/CLI — document if different).

## Native build

- [x] CMake (or MSBuild) project producing `link_shim.dll` (name TBD) for **x64** matching the app.
- [x] Exported functions: e.g. create/destroy context, enable/disable Link, app-thread capture returning tempo + optional peers, query beat time for host clock (match Link session-state API).
- [x] Document build steps for Windows (VS toolchain, CMake presets).
- [x] Optional: `native/README.md` with one-command build for contributors.

## Managed interop

- [x] P/Invoke declarations in a small type (Infrastructure or dedicated folder per [project-structure](../../agents/project-structure/README.md)).
- [x] Thin managed wrapper class `ILinkSession` / `LinkSessionNative` implementing IDisposable, safe handle pattern, no empty catch blocks.
- [x] **CI / dev without DLL:** `#if` or runtime probe — managed tests use **mock** or skip native tests when DLL missing (document).

## Tests

- [x] No hardware/network: unit tests against **mock** or pure C# façade where math is replicated.
- [x] Optional manual checklist: two machines or Rekordbox + app stub console printing tempo.

## Documentation

- [x] `docs/configuration-reference.md` or README: “Building with Link” prerequisites (not app settings yet).
- [x] Note firewall / LAN requirements for discovery (link to Ableton docs).
