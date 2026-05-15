# PBI-015: macOS + Windows host TFMs only (ADR-0086, no portable `net10.0` console)

**Status:** Implemented — pinned **`net10.0-macos26.0`** in repo root **`Directory.Build.props`** (`AudioAnalyzerMacOsHostTfm`); Console, Tests, Platform.macOS, CI, and docs aligned. **Transient work item** — close after merge.

## Directive

Migrate **AudioAnalyzer.Console** and **AudioAnalyzer.Tests** off the portable **`net10.0`** host so they multi-target **only** **`net10.0-windows10.0.19041.0`** (or the repo’s pinned **`-windows`** TFM) and a **pinned `net10.0-macos*`** TFM per [ADR-0086](../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md). Align **`AudioAnalyzer.Platform.macOS`** with the **same macOS TFM** as Console for the macOS graph (per ADR-0086 §2 — required for a consistent toolchain even before **ScreenCaptureKit** lands).

**In scope**

- **`dotnet workload install macos`** (and any **Xcode** floor) documented for contributors; pin **`TargetFramework`** / **`TargetFrameworks`** and **`Microsoft.macOS` Sdk** usage only if required for the chosen macOS TFM (minimal change: macOS TFM + project properties only if the SDK demands it).
- **Console**: conditional **`ProjectReference`** to **Platform.Windows** vs **Platform.macOS** per TFM; **`ServiceConfiguration`** / `#if` gates updated if the SDK defines different constants than today’s **`WINDOWS`-only** pattern (add documented **`MACOS`** or equivalent for the macOS TFM — follow .NET macOS SDK docs).
- **Tests**: mirror Console TFMs; fix conditional compile and test filters so **`dotnet test -f <macos TFM>`** passes on macOS CI with **0 warnings**.
- **`CreateMacOSAppBundle`** / **`Info.plist`** / output paths: replace hard-coded **`net10.0`** output folders with the **macOS TFM** path (or MSBuild properties so the path stays correct).
- **CI** ([`.github/workflows/build.yml`](../.github/workflows/build.yml)): macOS job builds/tests the **macOS TFM**; comments updated; Windows job unchanged unless restore graph requires tweaks.
- **Docs**: remove **“transitional `net10.0`”** wording from [README](../README.md), [docs/getting-started.md](../docs/getting-started.md), [docs/product-audience.md](../docs/product-audience.md), and [specs/platform-macos/spec.md](../specs/platform-macos/spec.md) **Implementation lag** bullet once migration is real; [AGENTS.md](../AGENTS.md) examples for macOS if they still say **`-f net10.0`**.
- **`EnableWindowsTargeting`** (and related) on projects that **restore** on macOS with Windows TFMs in the solution — verify after TFM change.

**Out of scope**

- **ScreenCaptureKit** implementation, new device rows, or **`Microsoft.macOS`** API usage beyond what the TFM requires for a green build — **PBI-016**.
- Ableton Link **dylib**, screen-dump Unix parity — unchanged backlog.

## Context pointer

- Primary spec: [`specs/platform-macos/spec.md`](../specs/platform-macos/spec.md)
- Related ADRs: [ADR-0086](../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md), [ADR-0084](../docs/adr/0084-macos-multi-target-and-platform-audio.md), [ADR-0085](../docs/adr/0085-macos-desktop-output-via-virtual-routing.md)
- Prior plumbing: [PBI-012](./PBI-012-macos-platform-foundation.md); audio behavior: [PBI-013](./PBI-013-macos-audio-input.md)

## Verification pointer

- **`dotnet build ./AudioAnalyzer.sln --no-incremental -warnaserror`** on **Windows**; **`dotnet build` / `dotnet test`** on **macOS** with **`-f <pinned macos TFM>`** for Console + Tests (unit filter per [AGENTS.md](../AGENTS.md)).
- Full suite + **`dotnet format ./AudioAnalyzer.sln --verify-no-changes`** per repo gates before merge.
- Spec **Definition of Done**: remove or satisfy the **Implementation lag** bullet for ADR-0086 in the same change set.

## Acceptance criteria

- Console and Tests **do not** list **`net10.0`** as a host TFM; macOS + Windows TFMs build and test as documented.
- macOS CI uses the **macOS** host TFM for build/test steps (no **`net10.0`** fallback for those projects).
- Operator docs show **`dotnet run` / `dotnet test`** with **`-f`** matching the pinned macOS TFM; `.app` bundle path instructions match output layout.

## Refinement rule

If pinning the macOS TFM forces a **higher minimum macOS** than today’s docs claim, update [docs/getting-started.md](../docs/getting-started.md) and the platform spec **Constraints** in the **same commit**.
