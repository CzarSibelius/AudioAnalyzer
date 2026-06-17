# PBI-016: macOS ScreenCaptureKit system audio (optional desktop capture)

> **Reverted / closed:** This work was implemented and later **removed** by [ADR-0088](../docs/adr/0088-macos-coreaudio-only-and-signed-app-bundle.md). macOS system audio now uses the **Core Audio process tap** only ([ADR-0087](../docs/adr/0087-macos-core-audio-tap-system-audio.md)); the ScreenCaptureKit and virtual-routing paths no longer exist. The directive below is **historical**.

**Transient work item** — close after merge (or split into follow-up PBIs if scope explodes).

## Directive

On the **macOS host TFM** delivered by **PBI-015**, add an **optional** **ScreenCaptureKit**-based path for **system / desktop output audio** so operators can visualize “what plays” **without** virtual routing when they accept **Screen Recording** consent. **Coexist** with [ADR-0085](../docs/adr/0085-macos-desktop-output-via-virtual-routing.md) (**virtual routing + heuristics** stay supported and must not be removed).

**In scope**

- **`IAudioInput`** (and related) implementation using Apple **ScreenCaptureKit** APIs available through **`Microsoft.macOS`** / workload bindings; packages must satisfy [ADR-0013](../docs/adr/0013-secure-nuget-packages.md) and [ADR-0075](../docs/adr/0075-nuget-license-compatibility.md).
- **Stable device id(s)** in **`AudioAnalyzer.Application`** (extend **`CrossPlatformAudioDeviceIds`** or add dedicated constants) so **`DeviceResolver`**, **`MacOsAudioDeviceInfo`**, and tests agree **without** project-reference cycles.
- **Device list**: new row(s) or clear labeling so operators distinguish **SCK system audio** vs **Desktop / virtual routing** vs microphones; update [specs/console-ui/device-selection-modal/spec.md](../specs/console-ui/device-selection-modal/spec.md) and help text per same-commit rule.
- **Consent and failure**: request permission at a deliberate point; **denied / revoked** behavior and logging per [ADR-0076](../docs/adr/0076-configurable-application-logging.md); documented fallback order (e.g. operator message → Demo or explicit routing row — **pick and document one** in spec + getting-started).
- **Threading / lifecycle**: match Apple requirements; no empty catch ([ADR-0011](../docs/adr/0011-no-empty-catch-blocks.md)).
- **Unit tests**: resolvers, id formatting, permission-denied branches with **mocks/fakes**; avoid requiring CI to grant screen recording.

**Out of scope**

- Replacing **Core Audio** microphone capture or **ADR-0085** heuristics.
- Windows changes beyond any shared **Application** id constants.
- **Notarized** distribution playbook (optional doc note only if product needs it).

## Context pointer

- Primary spec: [`specs/platform-macos/spec.md`](../specs/platform-macos/spec.md), [`specs/console-ui/device-selection-modal/spec.md`](../specs/console-ui/device-selection-modal/spec.md)
- Related ADRs: [ADR-0086](../docs/adr/0086-macos-windows-hosts-and-screencapturekit.md) §4, [ADR-0085](../docs/adr/0085-macos-desktop-output-via-virtual-routing.md)

## Verification pointer

- Manual test checklist in PR description (grant/deny/revoke Screen Recording; confirm audio reaches analysis).
- Automated: new/updated tests under `tests/AudioAnalyzer.Tests/` mirroring production layout [ADR-0064](../docs/adr/0064-test-project-mirrors-production-layout.md).
- **`dotnet build` / `dotnet test`** on macOS TFM and Windows TFM; **`dotnet format`** clean.

## Acceptance criteria

- Operator can select SCK-based desktop audio on supported macOS, see levels/visuals, and understand permission requirements from in-repo docs.
- With permission denied, app **does not crash** and behavior matches documented fallback.
- **ADR-0085** path still works unchanged for BlackHole / virtual routing users.

## Refinement rule

If Apple API surface or TFM constraints block “audio only” capture, document the minimum capture surface (e.g. display association) in the platform spec and ADR-0086 cross-note in the **same commit** — do not silently widen scope.

## Dependency

- **Blocked on** [PBI-015](./PBI-015-macos-windows-host-only-tfms.md) (macOS host TFM and Platform.macOS alignment) unless Dev explicitly prototypes SCK on a throwaway branch first.
