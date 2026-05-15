# AudioAnalyzer.Platform.macOS — folder layout

**`Audio/`**: **`MacOsAudioDeviceInfo`** (`IAudioDeviceInfo`) — Demo synthesis ids, a **Desktop / system output** synthetic row (`CrossPlatformAudioDeviceIds.MacOsDesktopVirtualRouting` from Application), optional **ScreenCaptureKit** system audio (`CrossPlatformAudioDeviceIds.MacOsScreenCaptureKitSystemAudio`, **`MacOsScreenCaptureKitSystemAudioInput`** + factory), plus **Core Audio** input enumeration (`MacOsCoreAudioEnumerator`, **`IMacOsAudioEnumerator`** for tests). **`MacOsCoreAudioAudioInput`** implements **`IAudioInput`** via AudioToolbox Audio Queue + PCM float normalization. **`MacOsAudioDeviceIds`** encodes persisted ids (`macos-input:` + escaped UID). **`MacOsDesktopMixSinkHeuristic`** tags common virtual desktop mixers (see [ADR-0085](../../adr/0085-macos-desktop-output-via-virtual-routing.md)).  

**`Audio/CoreAudio/`**: Native interop (AudioObjects, Audio Queue, CFString helpers); **no** third-party NuGet native stacks ([ADR-0084](../../adr/0084-macos-multi-target-and-platform-audio.md), [ADR-0075](../../adr/0075-nuget-license-compatibility.md)). Partial **`*.Logging.cs`** files pair with **`LoggerMessage`** where analyzers require it.

## Rules

- Keep Windows-only code out of this project.
- Prefer mirroring the abstraction shape from Application (`IAudioDeviceInfo` / `IAudioInput` → `Audio/` here).
- Non-macOS hosts must never invoke Core Audio: **`OperatingSystem.IsMacOS()`** gates enumeration/capture construction when shared types are referenced from tests; the **macOS platform assembly** itself targets the pinned **`net10.0-macos*`** host TFM only.
