# ADR-0085: macOS desktop (“system”) output via virtual routing and a stable list id

**Status**: Superseded by [0088](./0088-macos-coreaudio-only-and-signed-app-bundle.md)

> **Update (current):** The **virtual-routing** device row, the `CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting` id, and the desktop-mix sink heuristic described below were **removed** by [ADR-0088](./0088-macos-coreaudio-only-and-signed-app-bundle.md). macOS "what you hear" capture now uses the **Core Audio process tap** ([ADR-0087](./0087-macos-core-audio-tap-system-audio.md)) exclusively. Operators may still set up a BlackHole/Multi-Output device manually and select it as a normal **Core Audio input**, but the app no longer adds a dedicated routing row or applies sink heuristics. The sections below are **historical**.

## Context

On **Windows**, WASAPI exposes **render endpoints as loopback capture**, so operators pick **System Audio (Loopback)** or a per-device loopback row and hear the same program material the OS plays to speakers ([ADR-0084](./0084-macos-multi-target-and-platform-audio.md) contrasts this with macOS).

On **macOS**, Core Audio does **not** offer the same built-in loopback. The dominant operator workflow for “visualize what I hear” is still **routing**: install a **virtual input** (e.g. **BlackHole 2ch**), create a **Multi-Output Device** in **Audio MIDI Setup** that includes both the real output and the virtual device, set **Sound → Output** to that aggregate, then capture from the virtual device as a normal **input** in the app.

Operators migrating settings from Windows may persist **`InputMode: loopback`** with no Windows-specific device id; the macOS list historically lacked a **single obvious row** analogous to “System Audio (Loopback)”.

Native **ScreenCaptureKit** system-audio capture exists on newer macOS but would pull in **macOS-specific target frameworks** (e.g. `Microsoft.macOS` bindings), **Screen Recording** consent, and a separate capture stack from Core Audio **Audio Queue** inputs. That is intentionally **out of scope** for this ADR’s MVP; this ADR standardizes the **routing + discovery** path on **Core Audio** inputs. Optional SCK and **shipping host TFMs** are **[ADR-0086](./0086-macos-windows-hosts-and-screencapturekit.md)**.

## Decision

1. **Stable synthetic device id**  
   Define **`CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting`** in **Application** (`AudioAnalyzer.Application.Abstractions`) so **Console** (`DeviceResolver`) and **Platform.macOS** agree without a project-reference cycle.

2. **Device list UX**  
   **`MacOsAudioDeviceInfo.GetDevices`** includes a row after Demo modes: **Desktop / system output (virtual mixer if installed)** with that id. Selecting it means: **prefer the first enumerated Core Audio input** whose **name or UID** matches a small **heuristic** for common virtual sinks (BlackHole, Soundflower, Rogue Amoeba Loopback, etc.). If none match, **fall back to Demo synthesis (120 BPM)** and log a **warning** pointing operators to **docs/getting-started.md**.

3. **Heuristic labeling**  
   **`MacOsCoreAudioEnumerator`** tags matching physical inputs with **🔊** and **`(desktop mix)`** in the display name (plus **`HardwareName`** on **`MacOsPhysicalAudioDevice`** for heuristic checks) so virtual sinks are visually distinct from microphones.

4. **Settings resolution**  
   When **`AppSettings.InputMode == "loopback"`** and **`DeviceName`** is empty, **`DeviceResolver`** resolves to **`MacOsDesktopVirtualRouting`** if that entry exists in the list (after the Windows-only **`Id == null`** system-audio row).

5. **Future work**  
   Optional **ScreenCaptureKit**-based capture on the **macOS** host TFM, consent UX, and coexistence with this ADR’s routing path are specified in **[ADR-0086](./0086-macos-windows-hosts-and-screencapturekit.md)** (host targets **`net10.0-windows…`** + **`net10.0-macos*`** only—no portable **`net10.0`** console).

## Consequences

- **No new NuGet** dependencies for this ADR’s routing/heuristic MVP. **ScreenCaptureKit** may add packages per **[ADR-0086](./0086-macos-windows-hosts-and-screencapturekit.md)**. **Console/tests target frameworks** follow **0086** (superseding the historical portable **`net10.0`** host detail in [ADR-0084](./0084-macos-multi-target-and-platform-audio.md) for those projects).
- Operators without a virtual device still get **Demo** when choosing the desktop row; documentation remains the source of truth for **Multi-Output** setup.
- **Heuristic false positives/negatives** are possible for oddly named devices; operators can always pick the **specific** virtual input line when it appears.
- **Tests** cover the synthetic id, resolver behavior, and heuristic edge cases where practical.
