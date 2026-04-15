# ADR-0068: AGENTS.md vs README — audience split

**Status**: Accepted

## Context

The root `README.md` is the default entry point for people who clone or browse the repository. AI coding assistants and related tooling (Cursor, GitHub Copilot, and similar) conventionally load repository-level **`AGENTS.md`** as steering context.

Putting long agent-oriented workflows, build gates, and verification checklists into `README.md` blurs the audience: humans see noise meant for machines, and content is duplicated or hard to find in the right place.

## Decision

1. **Agent / machine instructions** — Prefer the repository root **`AGENTS.md`** as the primary entry, with deeper topic splits under **`docs/agents/`** and, where appropriate, **`.cursor/rules/`**. Examples: build must be zero-warnings, test commands, file placement, ADR reminders, verification checklists aimed at assistants.

2. **Human documentation** — Reserve **`README.md`** (especially at the repository root) for people: what the product is, who it is for, prerequisites, limitations, and short pointers to deeper docs. Step-by-step **build**, **run**, and **first session** (plus extended operator tips) live in **`docs/getting-started.md`**, linked from the README. A brief link from `README.md` to `AGENTS.md` for contributors using AI tools is fine.

3. **Folder-level `README.md` files** — When they serve as indexes or human guides (e.g. `docs/adr/README.md`), keep them human-oriented. Do not turn them into the primary agent playbook; link to `AGENTS.md` and `docs/agents/` instead.

4. **Optional nested `AGENTS.md`** — A subfolder or package may add its own `AGENTS.md` when local agent context is useful; this is optional and not required by this ADR.

## Update (current)

The root **`README.md`** prioritizes **product positioning**, **who it is for**, **prerequisites**, and **limitations**, with short links to deeper docs. Step-by-step **build**, **run**, **first launch**, **screen capture** CLI usage, and extended **tips / at-a-glance** for day-to-day use live in **`docs/getting-started.md`**; the README links there instead of duplicating those sections. **Agent** build/test/format gates and verification detail remain in **`AGENTS.md`** and **`docs/agents/`**.

## Consequences

- New content aimed at **AI agents** should not be added to the root `README.md`; extend **`AGENTS.md`** or the relevant **`docs/agents/*.md`** file.
- When the same facts would serve both audiences, **`README.md`** gets a short human summary and a link; full agent detail stays under **`AGENTS.md`** / **`docs/agents/`**.
- **`docs/agents/`** uses **`AGENTS.md`** for the topic index (agent-focused). A short **`docs/agents/README.md`** stub points to it so GitHub’s folder view still has a default landing page. **`docs/agents/project-structure/`** follows the same pattern: **`AGENTS.md`** for the placement spec, **`README.md`** stub linking to it.
