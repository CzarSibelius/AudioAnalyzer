# Spec template (AudioAnalyzer)

Copy this file to `specs/<kebab-feature-domain>/spec.md` when adding a new feature domain. This project follows the ASDLC **The Spec** pattern (spec-anchored): **Blueprint** = design constraints; **Contract** = verification and behavior. **Deterministic code remains the runtime source of truth** — the spec captures intent and invariants, not generated source.

**Repository gates** (agents): `dotnet build .\AudioAnalyzer.sln` with **0 warnings**; `dotnet test tests\AudioAnalyzer.Tests\AudioAnalyzer.Tests.csproj`; `dotnet format .\AudioAnalyzer.sln --verify-no-changes`. Use **PowerShell** on Windows. Architecture decisions live in **`docs/adr/`** — link them from **Architecture**, do not duplicate ADR prose.

**Same-commit rule**: If a change alters behavior visible to users or agents, update the relevant `specs/**/spec.md` in the **same** change.

---

## Blueprint

### Context

Why this feature exists; what problem it solves (one or two short paragraphs).

### Architecture

- **Code paths**: Concrete `src/...` and `tests/...` paths, main types, DI registration points.
- **Data / config**: JSON, `appsettings`, preset paths — reference [docs/configuration-reference.md](../docs/configuration-reference.md) when applicable.
- **ADRs**: Links to `docs/adr/NNNN-title.md` for decisions this feature must respect.

### Constraints

System rules stated as facts (viewport, encapsulation, platform, performance). Prefer **positive** rules (“all X uses Y”) per Living Specs; security-style negatives are OK when required.

---

## Contract

### Definition of Done

Observable, checkable criteria (build, tests, screen dumps, measurable behavior).

### Regression guardrails

Invariants that must survive every future change to this domain.

### Scenarios

```gherkin
Scenario: Example
  Given ...
  When ...
  Then ...
```

Use **Gherkin** for flows agents can verify; reference keys, toolbar labels, or ASCII layout as the observable surface.

---

## Legacy section mapping (migrated visualizer / UI docs)

When migrating older markdown under `docs/visualizers/` or `docs/ui-spec-*.md`:

| Legacy section | Maps to |
|----------------|---------|
| Title / intro paragraph | **Context** (and opening of **Architecture** if it is structural) |
| Description | **Context** |
| Snapshot usage | **Definition of Done** (checklist) and inputs to **Scenarios** |
| Settings | **Architecture** (schema, properties, JSON) |
| Key bindings | **Constraints** (interaction rules) and **Scenarios** |
| Viewport constraints | **Constraints** |
| Implementation notes | **Architecture** |
| Screenshot + line reference (UI specs) | **Architecture** or **Constraints** (“Visual reference”); line numbers remain the contract |

---

## State vs delta

| Artifact | Role |
|----------|------|
| `specs/<domain>/spec.md` | **State** — how the feature works and how we know it works |
| `tasks/PBI-001-*.md`, `PBI-002-*.md`, … (optional); **`tasks/PBI-000.md`** is the template | **Delta** — what to change for one delivery; must point at a spec and update the spec when contracts change |

Use the ASDLC MCP **knowledge base** only when helpful (e.g. refreshing [Adversarial Code Review](https://asdlc.io/practices/adversarial-code-review) for a Critic session). Repo **state** remains `specs/` and [AGENTS.md](../AGENTS.md); do **not** use the MCP for routine implementation or as a substitute for gates.
