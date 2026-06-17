# AudioAnalyzer.Platform.macOS — folder layout

**`Audio/`**: **`MacOsAudioDeviceInfo`** (`IAudioDeviceInfo`) — Demo synthesis ids, the **Core Audio tap** system-audio row (`CrossPlatformAudioDeviceIds.MacOsCoreAudioTapSystemAudio`, **`MacOsCoreAudioTapSystemAudioInputFactory`** / **`IMacOsCoreAudioTapSystemAudioInputFactory`** → **`MacOsCoreAudioTapAudioInput`**), plus **Core Audio** input enumeration (`MacOsCoreAudioEnumerator`, **`IMacOsAudioEnumerator`** for tests). Capture inputs implement **`IAudioInput`** via AudioToolbox Audio Queue + PCM float normalization. **`MacOsAudioDeviceIds`** encodes persisted ids (`macos-input:` + escaped UID). ScreenCaptureKit and the virtual-routing row/heuristic were **removed** ([ADR-0088](../../adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)). **Note:** the host must **not** call **`NSApplication.Init()`** via **`Microsoft.macOS.AppKit`** — that path loads **`__Internal.dylib`** and fails outside a proper app-bundle host ([dotnet/macios#18437](https://github.com/dotnet/macios/issues/18437)).

**`Audio/CoreAudioTap/`**: Core Audio process-tap system audio — **`MacOsSystemAudioCapture`** (`ISystemAudioCapture` over the native shim), **`MacOsAudioTapShimNative`** (P/Invoke into `libaudio_tap_shim.dylib`, searches `Contents/MacOS` in the `.app`), **`MacOsCoreAudioTapAvailability`** (OS support + capture readiness). See [ADR-0087](../../adr/0087-macos-core-audio-tap-system-audio.md), [ADR-0088](../../adr/0088-macos-coreaudio-only-and-signed-app-bundle.md).

**`Audio/CoreAudio/`**: Native interop (AudioObjects, Audio Queue, CFString helpers); **no** third-party NuGet native stacks ([ADR-0084](../../adr/0084-macos-multi-target-and-platform-audio.md), [ADR-0075](../../adr/0075-nuget-license-compatibility.md)). Partial **`*.Logging.cs`** files pair with **`LoggerMessage`** where analyzers require it.

**`AsciiVideo/`**: Webcam capture for the ASCII video text layer (mirrors `Platform.Windows/AsciiVideo/`) — **`MacOsAsciiVideoFrameSource`** (`IAsciiVideoFrameSource`) + **`MacOsAsciiVideoDeviceCatalog`** (`IAsciiVideoDeviceCatalog`) over the native shim, **`MacOsVideoCaptureShimNative`** (P/Invoke into `libvideo_capture_shim.dylib`, AVFoundation, searches `Contents/MacOS` in the `.app`), and **`MacOsCameraCaptureAvailability`** (OS support + capture readiness). Needs **`NSCameraUsageDescription`** in the bundle Info.plist for TCC. See [ADR-0074](../../adr/0074-ascii-video-layer-and-frame-source.md), [ADR-0088](../../adr/0088-macos-coreaudio-only-and-signed-app-bundle.md).

**`Hosting/`**: macOS implementations of cross-platform host abstractions ([ADR-0092](../../adr/0092-platform-behavior-via-abstractions-and-di-module.md)) — `NullScreenDumpContentProvider`, `MacOsConsoleBufferController` (no-op), `MacOsCapsLockState` (false), `MacOsHostContentLocator` (`.app` bundle Resources / Application Support), `MacOsStartupDiagnostics` (Core Audio tap availability logging).

**Project root**: `MacOsPlatformServiceCollectionExtensions.AddMacOsPlatform(...)` registers all of the above plus `MacOsAudioDeviceInfo` / `MacOsDefaultDeviceFallbackPolicy`; `MacOsPlatformOptions` is the test-override bag. Called from the console host's single OS switch (`PlatformSelection`).

## Rules

- Keep Windows-only code out of this project.
- Prefer mirroring the abstraction shape from Application (`IAudioDeviceInfo` / `IAudioInput` → `Audio/` here).
- Non-macOS hosts must never invoke Core Audio: **`OperatingSystem.IsMacOS()`** gates enumeration/capture construction when shared types are referenced from tests; the **macOS platform assembly** itself targets the pinned **`net10.0-macos*`** host TFM only.
