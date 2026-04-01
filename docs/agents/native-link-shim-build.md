# Native Link shim build (agents)

When you need **`link_shim.dll`** for Ableton Link BPM mode ([ADR-0066](../adr/0066-bpm-source-and-ableton-link.md)), follow this on **Windows**. The managed solution **does not require** the DLL to build or test; Link features activate only when the DLL is next to the executable.

## Toolchain (Option B: CMake + MSVC Build Tools)

1. **CMake** (≥ 3.16): install from [cmake.org/download](https://cmake.org/download/) (Windows x64). Prefer **Add CMake to the system PATH** during setup.

2. **C++ toolchain**: CMake does not compile code by itself. Install **[Build Tools for Visual Studio](https://visualstudio.microsoft.com/visual-cpp-build-tools/)** and the workload **Desktop development with C++** (MSVC + Windows 10/11 SDK). Full Visual Studio with the same workload is also fine.

3. **Build environment**: From a normal PowerShell, MSVC is often **not** on `PATH`. Use one of:

   - **Developer PowerShell for VS** or **x64 Native Tools Command Prompt for VS** (Start menu), or  
   - Run **`VsDevCmd.bat -arch=amd64`** from a [Developer Command Prompt](https://learn.microsoft.com/visualstudio/ide/reference/command-prompt-powershell) (path depends on install).

   Then confirm **`cmake --version`** works (or use the full path to `cmake.exe`).

## Steps (from repo root)

Use **PowerShell** on Windows per [AGENTS.md](../../AGENTS.md).

1. **Clone Ableton Link** into the path expected by CMake (directory is gitignored):

   ```powershell
   git clone --depth 1 https://github.com/Ableton/link.git native\third_party\link
   ```

   If CMake reports a missing `modules/asio-standalone/...` include path, initialize Link’s submodule (Asio):

   ```powershell
   cd native\third_party\link
   git submodule update --init --recursive
   cd ..\..
   ```

2. **Configure and build** the shim:

   ```powershell
   cd native\link-shim
   cmake -B build -A x64
   cmake --build build --config Release
   ```

3. **Verify** the artifact:

   ```powershell
   Test-Path .\build\Release\link_shim.dll
   ```

   Should return `True`.

4. **Deploy**: Copy **`native\link-shim\build\Release\link_shim.dll`** next to **`AudioAnalyzer.Console.exe`**, or run `dotnet build` on the solution after the DLL exists — [AudioAnalyzer.Console.csproj](../../src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj) may copy it to output when the file is present.

## Agent checklist

- [ ] CMake on PATH (or full path documented in the task).
- [ ] Build run from **Developer PowerShell for VS** (or equivalent) so MSVC is available.
- [ ] `native\third_party\link\include\ableton\Link.hpp` exists after clone.
- [ ] `native\link-shim\build\Release\link_shim.dll` exists after build.
- [ ] `dotnet build .\AudioAnalyzer.sln` still **0 warnings**; `dotnet test` passes with or without the DLL.

## License

Ableton Link and the shim are **GPL-2.0+**. Distribution of binaries must comply with the GPL (source offer, etc.).

## See also

- User-facing summary: [native/README.md](../../native/README.md)
- Settings and LAN: [docs/configuration-reference.md](../configuration-reference.md) (Ableton Link section)
