# Spec: Concurrent process instances (startup and logging)

**Repository gates** (agents): `dotnet build .\AudioAnalyzer.sln` with **0 warnings**; `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj`; `dotnet format .\AudioAnalyzer.sln --verify-no-changes`. Use **PowerShell** on Windows.

---

## Blueprint

### Context

Operators should be able to start **more than one** AudioAnalyzer process without startup failures. The known blocker is **file logging**: a shared default log path causes `IOException` on Windows when a second process opens the same file for writing. This spec defines the required behavior for **default** and **configured** log paths so multiple processes can run; it records **non-goals** for other shared persistence.

### Architecture

- **Code paths**: `src/AudioAnalyzer.Infrastructure/Logging/LogFilePathResolver.cs` (path resolution and placeholder expansion); `BackgroundFileLoggerProvider.cs` / `BackgroundFileLogWriter.cs` (open-after-resolve); `src/AudioAnalyzer.Console/appsettings.json` (default `Logging.FilePath`); `src/AudioAnalyzer.Domain/AppLoggingSettings.cs` (XML docs for `FilePath` placeholder syntax).
- **Tests**: `tests/AudioAnalyzer.Tests/` mirroring Infrastructure layout — unit tests for `LogFilePathResolver` covering placeholders, relative vs absolute paths, and empty default.
- **ADRs**: [ADR-0076](../../../docs/adr/0076-configurable-application-logging.md) (logging stack and performance); **[ADR-0083](../../../docs/adr/0083-concurrent-process-instances-and-log-files.md)** (concurrent instances and log path decision).

### Constraints

- Logging must remain **non-blocking** on hot paths per ADR-0076 (background writer unchanged in spirit).
- **No** reliance on multiple writers safely sharing one physical file without documented synchronization; per-process files are the supported default for concurrency.
- Placeholder expansion runs **once at startup** when the logger provider is created; no requirement for mid-run path switching.

---

## Contract

### Definition of Done

- With **default** shipped `Logging` configuration (enabled as today) and default path semantics, **two** processes launched from the same build output directory both **start successfully** (no `IOException` opening the log file).
- Each running process writes to a **distinct** default log file name that includes the **current process id** (or equivalent documented placeholder), under the configured `logs` folder relative to base directory unless an absolute path overrides it.
- `dotnet build` / `dotnet test` / `dotnet format --verify-no-changes` pass with **0 warnings**.
- [docs/configuration-reference.md](../../../docs/configuration-reference.md) describes `Logging.FilePath` placeholders and the explicit fixed-path caveat (same commit as behavior change, per same-commit rule).

### Regression guardrails

- Disabling file logging (`Logging.Enabled` false) must not require a log file path or writer.
- **Relative** and **absolute** `Logging.FilePath` values that **include** `{ProcessId}` resolve to a path containing the actual process id string.
- **Explicit** `Logging.FilePath` without placeholders: behavior remains **operator-defined** (may fail on second writer); no silent merge of logs.

### Scenarios

```gherkin
Scenario: Two processes start with default file logging
  Given file logging is enabled in appsettings
  And the default log path uses per-process resolution (e.g. contains expanded process id)
  When the user starts a second AudioAnalyzer process while the first is still running
  Then both processes start without IOException from the file log writer
  And each process writes to its own log file under the logs directory
```

```gherkin
Scenario: Log path resolver expands process id placeholder
  Given Logging.FilePath is "logs/audioanalyzer-{ProcessId}.log"
  When the application resolves the path at startup
  Then the resolved absolute path contains the numeric process id matching Environment.ProcessId
```

```gherkin
Scenario: Rooted log path with placeholder
  Given Logging.FilePath is an absolute path that includes "{ProcessId}" before the file extension
  When the application resolves the path at startup
  Then the resolved path is absolute and contains the process id
```

```gherkin
Scenario: Explicit single shared log path without placeholder (documented limitation)
  Given Logging.FilePath points to one fixed file path without "{ProcessId}"
  When a second process attempts to open that path for writing on Windows
  Then the product may fail to start or interleave logs
  And documentation states that single fixed paths are for single-instance or external tooling scenarios
```

### Out of scope (this spec)

- Concurrent **preset** or **appsettings** writes from multiple processes (no new locking or merge semantics required here).
- Changing **WASAPI** or **device** exclusivity rules for audio; this spec is only about **process startup** and **log file** path behavior as in ADR-0083.
