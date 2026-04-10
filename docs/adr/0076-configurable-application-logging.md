# ADR-0076: Configurable application logging

**Status**: Accepted

## Context

The application needs a **persistent diagnostic trail**, especially for **exceptions**, to support debugging and user reports. Today, failures in the visualization path are surfaced in the terminal viewport ([ADR-0012](0012-visualizer-exception-handling.md)) but are not written to durable storage. Console-only feedback is easy to miss and is lost when the session ends.

We also anticipate a need to tune **log verbosity** (for example Trace, Debug, Information) via configuration in a later phase, without changing code or rebuilding.

## Decision

1. **Introduce configurable application logging** driven from the same configuration source as other app settings (for example `appsettings.json`). The exact JSON shape for logging options is defined when implementation lands; it may use a conventional `Logging` section, nest under existing settings, or both, as long as it is documented in [docs/configuration-reference.md](../configuration-reference.md).

2. **MVP (first implementation)**  
   When logging is enabled, **persist application-captured exceptions** to a **configurable log file** with enough detail for diagnosis. At minimum each entry should include **timestamp**, **exception type**, **message**, and **stack trace**. Optional fields such as **category**, **operation name**, or **correlation context** are encouraged where low-cost.

3. **Relationship to viewport errors**  
   File logging **complements** [ADR-0012](0012-visualizer-exception-handling.md): the user-visible error in the viewport remains; the log file is **additive** for diagnostics.

4. **Implementation preference**  
   Standardize on **`Microsoft.Extensions.Logging`** with dependency injection (`ILogger<T>`, `ILoggerFactory`) per [ADR-0040](0040-dependency-injection-preference.md). Use a **file sink** implemented in-house or via a NuGet package whose license is verified under [ADR-0075](0075-nuget-license-compatibility.md); follow [ADR-0013](0013-secure-nuget-packages.md) for security and maintenance.

5. **Phase 2 (documented; not required for MVP)**  
   Support **configurable minimum log levels** (for example a global default and/or per-category overrides) and optional **additional sinks** (for example console). The precise schema and defaults are left to a follow-up change once MVP logging exists.

6. **Default behavior**  
   Prefer a **conservative default** (for example logging **disabled** or **errors-only**) so users are not surprised by disk use or sensitive data in log files unless they opt in. The chosen default must be stated in user-facing or configuration documentation when implemented.

## Consequences

- **Performance** ([ADR-0030](0030-performance-priority.md)): Writing to disk must not block audio, capture, or render hot paths. Prefer buffered, asynchronous, or background-thread flushing.

- **Exception handling** ([ADR-0011](0011-no-empty-catch-blocks.md)): Logging **supplements** meaningful handling (display, recovery, or explicit documented swallow). Do not replace required user feedback with log-only silent catches.

- **Privacy**: Log files may contain file paths, audio device names, or other environment-specific strings. Document this risk where configuration is described.

- **Configuration documentation**: When the logging schema is implemented, update [docs/configuration-reference.md](../configuration-reference.md) with paths, switches, and level rules.
