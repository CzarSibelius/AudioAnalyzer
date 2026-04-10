# ADR-0075: NuGet packages must comply with project license

**Status**: Accepted

## Context

The application is distributed under **GNU General Public License v3.0 only** (`GPL-3.0-only`); see the root `LICENSE`. NuGet dependencies are combined with our code in the built and distributed work. A package whose terms are incompatible with that distribution model creates legal and compliance risk. Contributors and AI agents need a single, explicit rule alongside security and maintenance policy ([ADR-0013](0013-secure-nuget-packages.md)).

## Decision

- **License compatibility**: Do not add or upgrade to a NuGet package unless its license terms are **compatible** with distributing this project as **GPL-3.0-only**—that is, the dependency may be linked or bundled such that the resulting distribution can still meet our obligations under our chosen license. This document is project policy, not legal advice; when unsure, obtain maintainer approval.
- **How to verify**: Check the package’s stated license on NuGet (license expression and linked license text) and the upstream repository if needed. When choosing libraries, prefer well-known permissive licenses that are widely treated as compatible with GPLv3 (e.g. MIT, BSD-family, Apache-2.0).
- **Out of scope without explicit approval**: Proprietary or other restricted terms that forbid redistribution or that conflict with copyleft distribution of the combined work. Unclear, missing, or dual-license cases: **stop and get a maintainer decision**—do not assume compatibility.
- **Relationship to other ADRs**: [ADR-0013](0013-secure-nuget-packages.md) (vulnerable/obsolete packages) applies in addition to this ADR whenever dependencies change. Native or vendored third-party pieces with their own license chains (e.g. Ableton Link and the native shim) are covered where applicable by [ADR-0066](0066-bpm-source-and-ableton-link.md); this ADR governs **NuGet** package choices for managed dependencies.
- **Enforcement**: This policy guides humans and agents when changing dependencies. CI may optionally add license or compliance checks; this ADR does not mandate a specific tool.

## Consequences

- New or updated NuGet dependencies require an explicit license check, not only “it builds” and ADR-0013 checks.
- An incompatible package must not be merged; a maintained alternative may be required.
- Agents and contributors consulting `docs/adr/` will see the rule when adding or changing NuGet dependencies.
