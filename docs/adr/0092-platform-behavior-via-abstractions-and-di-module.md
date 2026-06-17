# ADR-0092: Platform behavior behind abstractions with a single composition-root OS switch

**Status**: Accepted

## Context

The console host accumulated operating-system conditionals (`OperatingSystem.IsWindows()`, `OperatingSystem.IsMacOS()`, and `#if WINDOWS|MACOS`) spread across shared, cross-platform projects: the DI composition root ([ServiceConfiguration.cs](../../src/AudioAnalyzer.Console/ServiceConfiguration.cs)) branched per OS to register platform services; [Program.cs](../../src/AudioAnalyzer.Console/Program.cs) had `#if MACOS` startup logging and a runtime guard against the Windows audio stack on macOS; [HostContentPaths.cs](../../src/AudioAnalyzer.Infrastructure/HostContentPaths.cs) detected the macOS `.app` bundle; [HeaderContainer.cs](../../src/AudioAnalyzer.Console/Console/HeaderContainer.cs) branched for the Windows console buffer resize; `ConsoleShiftLetterV` read the Windows `Console.CapsLock`; `DeviceResolver` had a macOS-only Demo fallback; and `WindowsConsoleScreenDumpContentProvider` (Windows P/Invoke) lived in the cross-platform Console project with a runtime OS guard.

This conflicts with the platform split (per-TFM `ProjectReference` to `AudioAnalyzer.Platform.Windows` / `AudioAnalyzer.Platform.macOS`, see [ADR-0086](0086-macos-windows-hosts-and-screencapturekit.md), [ADR-0084](0084-macos-multi-target-and-platform-audio.md)) and with the DI preference ([ADR-0040](0040-dependency-injection-preference.md)): operating-system-specific behavior belongs inside the OS-specific project and should be injected, not branched on in shared code.

## Decision

1. **Platform behavior behind cross-platform abstractions.** Each piece of OS-specific behavior is expressed as an interface in `AudioAnalyzer.Application.Abstractions` and implemented per platform:
   - `IConsoleBufferController` — Windows resizes the console buffer; macOS no-op.
   - `ICapsLockState` — Windows reads `Console.CapsLock`; macOS reports false.
   - `IScreenDumpContentProvider` — Windows reads the console buffer via Win32 (`WindowsConsoleScreenDumpContentProvider`, moved into the Windows project); macOS uses `NullScreenDumpContentProvider`.
   - `IHostContentLocator` — macOS resolves `.app` bundle `Contents/Resources` + Application Support; Windows returns false (base directory).
   - `IPlatformStartupDiagnostics` — macOS logs Core Audio tap availability; Windows no-op.
   - `IDefaultDeviceFallbackPolicy` — macOS prefers Demo, Windows prefers the first device, when fresh loopback settings have no system-audio entry.

2. **Per-platform DI modules.** `AddWindowsPlatform(...)` (in `AudioAnalyzer.Platform.Windows`) and `AddMacOsPlatform(...)` (in `AudioAnalyzer.Platform.macOS`) register that platform's audio, now-playing, ASCII video, and the abstractions above. Test seams (now-playing / ASCII video overrides, and a macOS `MacOsPlatformOptions` bag) are passed as explicit parameters so the shared `ServiceConfigurationOptions` carries no OS-specific types.

3. **A single compile-time OS switch.** All `#if WINDOWS|MACOS` is confined to one file, [PlatformSelection.cs](../../src/AudioAnalyzer.Console/PlatformSelection.cs), which selects the platform module (`AddPlatformServices`) and the pre-DI content locator (`CreateContentLocator`). `ServiceConfiguration`, `Program`, `HostContentPaths`, `HeaderContainer`, `ConsoleShiftLetterV`, and `DeviceResolver` contain no operating-system conditionals.

4. **OS checks remain only inside the OS-specific projects.** Guards such as `OperatingSystem.IsMacOS()` inside `AudioAnalyzer.Platform.macOS` stay (several are required by analyzer `CA1416`); they are correct because they live in the OS-specific project.

## Consequences

- Shared projects (`Console` except `PlatformSelection`, `Infrastructure`, `Application`) are free of OS conditionals; adding a platform behavior means adding an abstraction + per-platform implementations + a registration in the platform module, not a new branch in shared code.
- The macOS-on-Windows-stack runtime guard and `MacOsLaunchDiagnostics.ReportWindowsAudioStackOnMacOsAndExit` were removed as unreachable: only the matching platform module is referenced/registered per TFM. `BootstrapLogging` moved into `MacOsStartupDiagnostics`.
- `HostContentPaths.Resolve` now takes an `IHostContentLocator`; `DeviceResolver.TryResolveFromSettings` takes an `IDefaultDeviceFallbackPolicy`; `HeaderContainer` takes an `IConsoleBufferController`; `ConsoleShiftLetterV` is an injectable instance taking `ICapsLockState`.
- The platform projects gain a `Microsoft.Extensions.DependencyInjection` reference for the module extension methods.
- Updates [ADR-0046](0046-screen-dump-ascii-screenshot.md): the Windows screen-dump provider now lives in `AudioAnalyzer.Platform.Windows` and macOS uses a null provider, both injected (no runtime OS guard).
