# ADR-0013: Avoid insecure or obsolete NuGet packages

**Status**: Accepted

## Context

Insecure or outdated NuGet packages introduce vulnerabilities (CVEs), compatibility risks, and maintenance burden. The project uses packages such as NAudio, SixLabors.ImageSharp, Microsoft.Extensions.DependencyInjection, and Roslynator.Analyzers. Contributors and AI agents may add or update packages; a clear policy ensures consistency, security, and long-term maintainability.

## Decision

- **Insecure packages**: Do not use packages with known critical or high vulnerabilities (CVEs). Resolve such vulnerabilities before merging.
- **Obsolete packages**: Prefer packages that are actively maintained and within support. Avoid deprecated or end-of-life packages when a suitable maintained alternative exists.
- **Verification**: Use built-in tooling to check:
  - `dotnet list package --vulnerable` — lists packages with known vulnerabilities
  - `dotnet list package --outdated` — lists packages with newer versions
- **Adding new packages**: Before adding a package, check for known vulnerabilities and recency of updates (e.g., on nuget.org or via the above commands).
- **Guidance over enforcement**: This policy guides humans and agents when changing dependencies. CI may optionally run vulnerability checks; the ADR does not mandate a specific enforcement mechanism.

## Consequences

- Package additions and upgrades must align with this policy.
- Critical and high vulnerabilities must be addressed before merging.
- Agents and contributors consulting `docs/adr/` will see the rule when changing NuGet dependencies.
- Trade-off: Some older packages may be necessary for compatibility; the policy allows judgment when no suitable maintained alternative exists, but vulnerabilities must still be resolved.
