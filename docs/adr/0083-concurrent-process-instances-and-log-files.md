# ADR-0083: Concurrent process instances and log files

**Status**: Accepted

## Context

Users run **more than one** AudioAnalyzer process at the same time (separate terminals, comparisons, automation, or accidental double launch). Today, when **file logging** is enabled, each process resolves the same default path (for example `logs/audioanalyzer.log` under the app base directory) and opens it for append with a share mode that **does not allow a second writer** on Windows. The second process fails during logger construction with `System.IO.IOException` and exits before the UI starts.

This is a **startup hard failure**, not a degraded mode. It is independent of audio devices or visualization; it blocks the entire application.

[ADR-0076](0076-configurable-application-logging.md) established file-backed logging, non-blocking writes, and configuration shape. It did not require **multi-writer** or **multi-process** semantics for the log file path.

## Decision

1. **Product expectation**: Running **multiple processes** of the application on the same machine (same base directory and configuration) is **supported** when file logging is enabled, without requiring users to hand-edit paths for each launch.
2. **Default log path is process-scoped**: The effective physical log file used when logging is enabled **must not** be a single shared default path that only one process can open for writing. **Path resolution** (see `LogFilePathResolver` and configuration in [docs/configuration-reference.md](../configuration-reference.md) when updated) **must** support deterministic **placeholders** expanded at startup, at minimum:
   - **`{ProcessId}`** — numeric process id of the current process (required for MVP of this ADR).
   - Optional later: **`{Guid}`** or similar for sub-process or advanced scenarios, if a concrete need appears; not required for the MVP described here.
3. **Configuration defaults**: The shipped default `Logging.FilePath` (and the resolver behavior when the path is empty) **must** expand to a **per-process** file name (for example `logs/audioanalyzer-{ProcessId}.log` after resolution). Implementations **must** document placeholder syntax alongside `AppLoggingSettings.FilePath`.
4. **Explicit fixed paths**: If an operator sets `Logging.FilePath` to a **fixed** path **without** a process-scoped placeholder, the application **does not** guarantee multi-process behavior: the OS may deny a second writer or interleaved writes may occur. This is **acceptable**; document it as an operator choice for single-instance or external log aggregation setups.
5. **Interleaving and locking**: **Do not** rely on multiple processes appending to one file without cross-process synchronization; that invites torn log lines. Prefer **separate files per process** via placeholders over widening `FileShare` alone.
6. **Relationship to ADR-0076**: Non-blocking background writes, minimum levels, and complementing [ADR-0012](0012-visualizer-exception-handling.md) remain unchanged. This ADR **narrows** path resolution and default configuration so concurrent instances satisfy the product expectation above.

## Consequences

- **Configuration docs**: Update [docs/configuration-reference.md](../configuration-reference.md) when implementation lands: placeholder list, default path, and the explicit-path / single-writer caveat.
- **Tests**: Add unit coverage for path resolution (placeholder expansion, rooted vs relative paths, empty path default).
- **User-facing log discovery**: Support and docs should mention that logs may be named per process id so users know where to look when reporting issues.
- **Other shared files** (presets, `appsettings`, themes): Concurrent edits from multiple processes are **out of scope** for this ADR; they may still race. Future work can document or harden those separately; this ADR only removes the **logging file lock** startup failure for the default case.
