# ADR-0040: Prefer dependency injection unless performance requires otherwise

**Status**: Accepted

## Context

The codebase uses dependency injection (DI) for visualizers, layers, modals, the title bar, and scrolling viewports (see ADR-0008, ADR-0028, ADR-0035, ADR-0036, ADR-0037). This improves testability, reduces coupling, and keeps components self-contained. However, there is no overarching principle for when to apply DI versus when to use static helpers, direct construction, or other approaches. Developers and agents need clear guidance: default to DI, but avoid it when it would harm performance (ADR-0030).

## Decision

1. **Prefer dependency injection by default**. When adding or refactoring components (services, UI components, visualizers, layers, modals, providers), use constructor injection, register in the DI container, and inject into consumers. Follow existing patterns: interfaces in `Abstractions/`, implementations in their respective assemblies, registration in ServiceConfiguration.

2. **Exception: performance-critical hot paths**. When a component runs on a hot path (e.g. per-frame rendering, audio processing, high-frequency polling), and profiling shows that DI resolution or extra indirection causes measurable overhead, it is acceptable to deviate:
   - Use static helpers or direct construction where avoiding allocations or indirection is justified.
   - Document the rationale (e.g. "Static to avoid per-frame service resolution per ADR-0040").
   - Prefer singleton or scoped registrations for hot-path services so resolution cost is paid once, not per frame.

3. **When in doubt, use DI**. Only opt out after measurement. ADR-0030 encourages profiling over guesswork; the same applies here.

## Consequences

- New components default to DI; exceptions require justification and documentation.
- Performance-related exceptions align with ADR-0030; both ADRs reinforce measurement before optimization.
- Existing DI ADRs (0008, 0028, 0035, 0036, 0037) remain the primary references for implementation patterns; this ADR establishes the general principle.
- Agents and developers have a clear default: inject dependencies unless performance evidence says otherwise.
