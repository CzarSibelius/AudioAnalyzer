# Native components

## Core Audio tap shim (`audio-tap-shim`)

macOS **14.2+** system/output audio capture for the **Core Audio tap** device row ([ADR-0087](../docs/adr/0087-macos-core-audio-tap-system-audio.md)). The app loads **`libaudio_tap_shim.dylib`** from **`Contents/MacOS`** inside the `.app` bundle (the finalize step copies it there). Builds and tests succeed **without** the dylib; on supported macOS versions the tap list entry is still shown with a **build the shim** label until the library is present and loads.

### Build (macOS)

From **`native/audio-tap-shim`** (Xcode command-line tools required):

```bash
SDK="$(xcrun --sdk macosx --show-sdk-path)"
mkdir -p build
clang++ -std=c++17 -fobjc-arc -dynamiclib -o build/libaudio_tap_shim.dylib audio_tap_shim.mm \
  -framework CoreAudio -framework AudioToolbox -framework Foundation \
  -DAUDIO_TAP_SHIM_EXPORTS -isysroot "$SDK" -mmacosx-version-min=14.2
```

Or, when **CMake** is on `PATH`:

```bash
cd native/audio-tap-shim
cmake -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
```

Once **`build/libaudio_tap_shim.dylib`** exists, run the macOS console via **`scripts/macos/run.sh`** (or `dotnet run -f net10.0-macos26.0`). The **`FinalizeMacOsAppBundle`** step (`scripts/macos/pack-bundle.sh`) copies the dylib into **`Contents/MacOS`** of the `.app`, injects the privacy usage strings, and **ad-hoc re-signs** the bundle so macOS TCC can grant consent ([ADR-0088](../docs/adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)). Grant **System Audio Recording** when prompted (or enable it for **AudioAnalyzer** in System Settings → Privacy & Security).

**Ad-hoc TCC caveat:** the ad-hoc signature changes on every rebuild, so macOS may treat the rebuilt bundle as a new app and **re-prompt or require re-toggling** Microphone / System Audio Recording consent. To verify a finalized bundle:

```bash
APP="src/AudioAnalyzer.Console/bin/Debug/net10.0-macos26.0/osx-arm64/AudioAnalyzer.Console.app"
codesign --verify --strict --verbose=2 "$APP"
/usr/libexec/PlistBuddy -c 'Print :NSAudioCaptureUsageDescription' "$APP/Contents/Info.plist"
ls "$APP/Contents/MacOS/libaudio_tap_shim.dylib"
```

## Video capture shim (`video-capture-shim`)

macOS **webcam capture** for the **ASCII video** text layer ([ADR-0074](../docs/adr/0074-ascii-video-layer-and-frame-source.md)). The app loads **`libvideo_capture_shim.dylib`** from **`Contents/MacOS`** inside the `.app` bundle (the finalize step copies it there). Builds and tests succeed **without** the dylib; the ASCII video layer simply shows *No camera* and the webcam device list is empty until the library is present and loads.

### Build (macOS)

From **`native/video-capture-shim`** (Xcode command-line tools required):

```bash
SDK="$(xcrun --sdk macosx --show-sdk-path)"
mkdir -p build
clang++ -std=c++17 -fobjc-arc -dynamiclib -o build/libvideo_capture_shim.dylib video_capture_shim.mm \
  -framework AVFoundation -framework CoreMedia -framework CoreVideo -framework Foundation \
  -DVIDEO_CAPTURE_SHIM_EXPORTS -isysroot "$SDK" -mmacosx-version-min=14.0
```

Or, when **CMake** is on `PATH`:

```bash
cd native/video-capture-shim
cmake -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build --config Release
```

Once **`build/libvideo_capture_shim.dylib`** exists, run the macOS console via **`scripts/macos/run.sh`** (or `dotnet run -f net10.0-macos26.0`). The **`FinalizeMacOsAppBundle`** step (`scripts/macos/pack-bundle.sh`) copies the dylib into **`Contents/MacOS`** of the `.app`, injects the **`NSCameraUsageDescription`** privacy usage string, and **ad-hoc re-signs** the bundle so macOS TCC can grant consent ([ADR-0088](../docs/adr/0088-macos-coreaudio-only-and-signed-app-bundle.md)). Grant **Camera** access when prompted (or enable it for **AudioAnalyzer** in System Settings → Privacy & Security → Camera). The **ad-hoc TCC caveat** described above (re-prompts on rebuild) applies here too.

## Ableton Link shim (`link-shim`)

The managed app loads `**link_shim.dll**` from the executable directory to use **Ableton Link** as a BPM source ([ADR-0066](../docs/adr/0066-bpm-source-and-ableton-link.md)). The .NET solution **builds and tests without** this DLL; Link mode shows a hint until the DLL is present.

### Prerequisites (Windows)

- **CMake** (≥ 3.16) on `PATH` — [cmake.org/download](https://cmake.org/download/)
- **MSVC + Windows SDK** — e.g. [Visual Studio Build Tools](https://visualstudio.microsoft.com/visual-cpp-build-tools/) with **Desktop development with C++**
- **Build from a VS developer environment** (CMake alone is not enough): **Developer PowerShell for VS** or **x64 Native Tools Command Prompt**, so the compiler is on `PATH`

**Agent instructions** (checklist, verification, PowerShell commands): **[docs/agents/native-link-shim-build.md](../docs/agents/native-link-shim-build.md)**.

### Quick build

1. Clone the official Link repository into `**third_party/link`** (ignored by git), from the **repository root**:
  ```powershell
   git clone --depth 1 https://github.com/Ableton/link.git native\third_party\link
  ```
   Then pull the Asio submodule (required for CMake):
2. Configure and build (from `**native/link-shim**`, in Developer PowerShell for VS):
  ```powershell
   cd native\link-shim
   cmake -B build -A x64
   cmake --build build --config Release
  ```
3. Copy `**build/Release/link_shim.dll**` next to `AudioAnalyzer.Console.exe`, or run `dotnet build` on the solution — [AudioAnalyzer.Console.csproj](../src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj) copies the DLL to output **if** `native\link-shim\build\Release\link_shim.dll` already exists.

**License:** Ableton Link and this shim are under **GPL-2.0+**. Distribution requires compliance with the GPL (source offer, etc.). The rest of the Audio Analyzer repository is **GPL-3.0-only** — see the root `LICENSE`.