# Native components

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