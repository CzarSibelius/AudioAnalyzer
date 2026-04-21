# Agentic personas (Lead, Dev, Critic)

This repository follows ASDLC guidance **Agent Personas**: full persona definitions are **session-scoped** Cursor project skills so they are not loaded on every agent turn via bloated root instructions. Root [AGENTS.md](../../AGENTS.md) holds a short **registry** only.

## When to use which persona

| Persona | Attach this skill | Use for |
| ------- | ----------------- | ------- |
| **Lead** | [persona-lead](../../.cursor/skills/persona-lead/SKILL.md) | Specs under `specs/`, ADR intent, PBIs, planning — before implementation. |
| **Dev** | [persona-dev](../../.cursor/skills/persona-dev/SKILL.md) | C#, tests, config — implementing an agreed spec or PBI. |
| **Critic** | [persona-critic](../../.cursor/skills/persona-critic/SKILL.md) | Skeptical review vs spec, ADRs, and build/test/format gates — findings only, no fixes. |

**Handoff:** Lead defines intent → Dev implements → Critic verifies (or sends back).

## Adversarial Code Review (ASDLC)

This repo implements the **Adversarial Code Review** pattern: a **Critic** pass that is skeptical by design and validates **Builder** output against contracts **before** you treat a change as merge-ready.

| Step | What to do in AudioAnalyzer |
|------|------------------------------|
| 1. Build | **persona-dev** (or default agent) implements from `tasks/PBI-001-*.md` (and higher numbers; not `PBI-000.md`) and/or `specs/**/spec.md`. |
| 2. Context swap | Run the review in a **new chat** or a **readonly subagent** so the Critic does not inherit the Builder’s reasoning trace (same model is fine; separation of session matters). |
| 3. Critique | Attach **persona-critic** and supply the **diff** plus the **relevant spec files** and [AGENTS.md](../../AGENTS.md) (constitution). Compare Blueprint + Contract (DoD, scenarios, guardrails). |
| 4. Verdict | Critic outputs **PASS** or **NOT READY TO MERGE** with cited violations and remediation — no code edits in that persona. |

Optional **parallel lanes** (Architect / QA / SecOps style) are methodology-only; if you use them, add a **moderator** step that deduplicates findings before sending work back to Dev.

## ASDLC MCP (knowledge base)

Enable the **Agentic Software Development Lifecycle** MCP in [`.cursor/mcp.json`](../../.cursor/mcp.json) (remote URL `https://asdlc.io/mcp`) so agents can call the official knowledge base tools:

| Tool | Use |
|------|-----|
| `search_knowledge_base` | Find articles by topic; read returned **slugs** only. |
| `get_article` | Fetch full markdown for one slug from search results — **do not guess slugs**. |
| `list_articles` | Browse the catalog when search is too narrow. |

**When to use it:** align a Critic session with canonical pattern text (e.g. search for “adversarial code review”, then `get_article` with the **exact** `slug` from the search result — often `adversarial-code-review`). **When not to:** routine coding, replacing `specs/`, or skipping `dotnet build` / tests / `dotnet format`.

Operational gates and **The Spec** remain in [AGENTS.md](../../AGENTS.md), `specs/`, and `docs/agents/`.
