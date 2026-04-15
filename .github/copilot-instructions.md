# AudioAnalyzer — GitHub Copilot / AI assistant instructions

> **Scope**: Integrations that read **`.github/copilot-instructions.md`** (e.g. GitHub Copilot) should treat this file as a **thin entry point**. **Canonical agent steering** is **[`AGENTS.md`](../AGENTS.md)** at the repository root, with detailed topics under **`docs/agents/`**. Prefer changing `AGENTS.md` and `docs/agents/*.md` instead of growing this file (see [ADR-0068](../docs/adr/0068-agents-md-vs-readme.md)).

## Follow these first

1. Open **[`AGENTS.md`](../AGENTS.md)** — .NET 10 SDK, build (`dotnet build .\AudioAnalyzer.sln`, **0 warnings**), tests, format, ADR pointer, and the topic index table.
2. Before architectural or user-facing changes, read **[`docs/adr/README.md`](../docs/adr/README.md)** and align with accepted ADRs. Condensed ADR reminders for agents live in **[`.cursor/rules/adr.mdc`](../.cursor/rules/adr.mdc)** (Cursor loads rules automatically; open that file when working from other tools).

## Working in this repo (short summary)

- **Windows / PowerShell**: Do not use Unix utilities (`head`, `grep`, …) in shell examples; use PowerShell (e.g. `Select-Object -First N`).
- **Layout**: New or moved code follows **`docs/agents/project-structure/`**; tests mirror production per **ADR-0064**.
- **Audience**: Human docs in **`README.md`** (product/audience summary) and **`docs/getting-started.md`** (build, run, first session); agent docs in **`AGENTS.md`** and **`docs/agents/`** — see **ADR-0068** above.

Do not restore long verification checklists, ADR enumerations, or legacy architecture notes here; update **`AGENTS.md`**, **`docs/agents/`**, or **`.cursor/rules/`** instead.
