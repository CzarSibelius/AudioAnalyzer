# ADR-0086: macOS and Windows console hosts (no portable `net10.0`) and optional ScreenCaptureKit desktop audio

**Status**: Accepted (ScreenCaptureKit portion superseded by [0088](./0088-macos-coreaudio-only-and-signed-app-bundle.md))

> **Update (current):** The **host TFM policy** in this ADR (Console + Tests target `net10.0-windows…` and `net10.0-macos*` only, no portable `net10.0`) **remains in force**. The optional **ScreenCaptureKit** desktop-audio path (Decision §4 and related consequences) was **removed** by [ADR-0088](./0088-macos-coreaudio-only-and-signed-app-bundle.md): macOS system audio now uses the **Core Audio process tap** ([ADR-0087](./0087-macos-core-audio-tap-system-audio.md)) only, and the macOS console runs from an **ad-hoc signed `.app` bundle** so TCC grants Microphone / System Audio Recording. Treat the SCK material below as **historical**.

## Context

[ADR-0084](./0084-macos-multi-target-and-platform-audio.md) introduced a **three-way** console story: portable **`net10.0`**, full **`net10.0-windows…`**, and platform-selected references for audio. That kept Linux/macOS CI and contributors on a **single portable TFM** without Windows APIs.

Operators still want **macOS “what you hear”** without always installing **virtual audio** ([ADR-0085](./0085-macos-desktop-output-via-virtual-routing.md)). Apple’s **ScreenCaptureKit** can capture **system audio** on supported macOS versions, but it requires **macOS-targeted bindings** (e.g. **`Microsoft.macOS`** / **`macos` workload**), **Screen Recording** consent UX, and code paths that are **not** expressible on a plain **`net10.0`** class library graph for the host.

We accept that **AudioAnalyzer.Console** (and **AudioAnalyzer.Tests** mirroring it per [ADR-0064](./0064-test-project-mirrors-production-layout.md)) do **not** need to run as **`dotnet run` on generic `net10.0`** for product delivery: a **Windows** build and a **macOS** build are sufficient. Shared libraries (**Domain**, **Application**, **Infrastructure**, **Visualizers**, etc.) may remain **`net10.0`** where they stay platform-agnostic.

## Decision

1. **Host target frameworks**  
   **Console** and **Tests** multi-target **exactly two** product TFMs (no portable **`net10.0`** host):
   - **`net10.0-windows10.0.19041.0`** (or a later pinned **`-windows`** TFM we standardize on) for the Windows graph.
   - **`net10.0-macos*`** — a **versioned macOS TFM** chosen at implementation time to match **`Microsoft.macOS`** / **`dotnet workload install macos`** and the installed .NET SDK’s **valid TargetPlatformVersion list** (e.g. .NET 10 may accept **`net10.0-macos26.0`** / **`net10.0-macos26.4`** while rejecting older monikers); the repo pins **`AudioAnalyzerMacOsHostTfm`** in **`Directory.Build.props`** — treat the exact suffix as an implementation pin, not this ADR’s variable.

2. **Platform assemblies**  
   - **`AudioAnalyzer.Platform.Windows`** stays on the **Windows** TFM.  
   - **`AudioAnalyzer.Platform.macOS`** targets the **same macOS TFM** as the macOS console build so tooling, workloads, and future **ScreenCaptureKit** / **`Microsoft.macOS`** interop stay aligned ([`Directory.Build.props`](../Directory.Build.props) → **`AudioAnalyzerMacOsHostTfm`**).

3. **Conditional references**  
   Console references **exactly one** platform assembly per TFM (never both in one build). **`#if WINDOWS`** (or equivalent SDK-defined constants for the Windows TFM) vs macOS TFM symbols gate registrations in **`ServiceConfiguration`** as today. **Infrastructure** must not take unconditional WASAPI or SCK dependencies.

4. **ScreenCaptureKit (optional product path)**  
   On the **macOS** host TFM, we **may** implement **system-wide desktop / output audio** via **ScreenCaptureKit** as an alternative or complement to **Core Audio** input + **virtual routing** ([ADR-0085](./0085-macos-desktop-output-via-virtual-routing.md)):
   - **Consent**: operators must understand **Screen Recording** permission; denied or revoked consent must degrade **predictably** (documented fallback: e.g. **Demo**, explicit **virtual routing** row, or a **specific microphone**—exact order in spec/docs at implementation).
   - **Minimum capture surface**: Apple’s APIs require a display-scoped **content filter** even when the product path consumes **audio only**; implementations must document that the filter may capture display-associated content while forwarding **PCM for analysis only** (see `specs/platform-macos/spec.md` and operator docs).
   - **Device list and ids**: use stable **Application**-level ids (extend **`CrossPlatformAudioDeviceIds`** or add SCK-specific ids) so **Console** resolution and **Platform.macOS** agree; avoid project-reference cycles.
   - **Coexistence with ADR-0085**: **virtual routing + heuristics** remain a **supported** path for operators who prefer not to grant Screen Recording or who already use **BlackHole** / aggregates. Do not remove that path solely because SCK exists.

5. **CI and cross-compilation**  
   - **Windows** job: build and test **`-windows`** TFM (full suite expectations unchanged unless documented).  
   - **macOS** job: build and test **macOS** TFM; **`EnableWindowsTargeting`** (or successor knobs) remains on projects that must **restore** Windows TFMs from non-Windows agents.  
   - **Linux** agents: if they only build **solution** projects, either exclude console/tests from default build configuration for Linux or document **`-f`** limitations—**Linux is not a supported runtime host** for the product console unless we reintroduce a portable TFM later (out of scope here).

6. **Packages**  
   Any **`Microsoft.macOS`** (or related) packages must satisfy [ADR-0013](./0013-secure-nuget-packages.md) and [ADR-0075](./0075-nuget-license-compatibility.md).

## Consequences

- **No portable `net10.0` console** on macOS or Windows for shipping: local **`dotnet run -f …`** must pick the OS-appropriate TFM; documentation ([`docs/getting-started.md`](../getting-started.md), root **README**) must state that clearly when migration lands.
- **SCK** adds **permission UX**, **manual test** burden, and possibly **entitlements / bundling** concerns for **`.app`** distribution; automated tests stay focused on **resolvers, ids, and fallbacks** with heavy mocking—**not** live capture.
- **ADR-0085** remains the canonical description of **virtual routing**; this ADR adds **host TFM policy** and the **hook** for **SCK** without duplicating heuristic details.
- **ADR-0084** remains canonical for **WASAPI out of Infrastructure** and **platform-owned audio**; the **portable `net10.0` host** detail is **replaced** for Console/Tests by this ADR’s **two host TFMs** (see **Relationship** in ADR-0084).

## Implementation tracking

- **Host TFM migration** (Console, Tests, Platform.macOS, CI, docs): [`tasks/PBI-015-macos-windows-host-only-tfms.md`](../../tasks/PBI-015-macos-windows-host-only-tfms.md)
- **ScreenCaptureKit** system audio (PBI-016): implemented historically, then **removed** by [ADR-0088](./0088-macos-coreaudio-only-and-signed-app-bundle.md) in favor of the Core Audio process tap; the `MacOsScreenCaptureKitSystemAudioInput` types and `MacOsScreenCaptureKitSystemAudio` id no longer exist.
