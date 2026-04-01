# Phase 2: Link library and managed wrapper (standalone)

**Outcome:** A **native shim DLL** (or agreed alternative) wraps [Ableton Link](https://ableton.github.io/link/) and exposes a **minimal C ABI**. Managed code can **load**, **enable**, **read session tempo**, **peer count**, and **beat-at-time** (or equivalent) for future Phase 3 — **without** yet changing app BPM source behavior.

**Prerequisite:** Phase 1 complete (optional but recommended so Phase 3 only adds `LinkBeatTimingSource`).

## Repository layout and license

- [ ] Add git **submodule** for `https://github.com/Ableton/link` under e.g. `native/ableton-link/link/` (exact path documented in README).
- [ ] Document **GPL-2.0+** obligations in `README.md` and in ADR from Phase 1/3 (source offer, distribution).
- [ ] Choose integration approach (default: **C shim + P/Invoke**; alternatives: C++/CLI — document if different).

## Native build

- [ ] CMake (or MSBuild) project producing `link_shim.dll` (name TBD) for **x64** matching the app.
- [ ] Exported functions: e.g. create/destroy context, enable/disable Link, app-thread capture returning tempo + optional peers, query beat time for host clock (match Link session-state API).
- [ ] Document build steps for Windows (VS toolchain, CMake presets).
- [ ] Optional: `native/README.md` with one-command build for contributors.

## Managed interop

- [ ] P/Invoke declarations in a small type (Infrastructure or dedicated folder per [project-structure](../../agents/project-structure/README.md)).
- [ ] Thin managed wrapper class `ILinkSession` / `LinkSessionNative` implementing IDisposable, safe handle pattern, no empty catch blocks.
- [ ] **CI / dev without DLL:** `#if` or runtime probe — managed tests use **mock** or skip native tests when DLL missing (document).

## Tests

- [ ] No hardware/network: unit tests against **mock** or pure C# façade where math is replicated.
- [ ] Optional manual checklist: two machines or Rekordbox + app stub console printing tempo.

## Documentation

- [ ] `docs/configuration-reference.md` or README: “Building with Link” prerequisites (not app settings yet).
- [ ] Note firewall / LAN requirements for discovery (link to Ableton docs).
