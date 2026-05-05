# PBI-014: macOS operator docs and console parity

**Transient work item** — close after merge.

## Directive

Make macOS support **discoverable and honest** for operators, and align console-adjacent behaviors with ADR-0084 outcomes.

**In scope**

- **Documentation**: [README.md](../README.md), [docs/getting-started.md](../docs/getting-started.md), [docs/product-audience.md](../docs/product-audience.md), [docs/configuration-reference.md](../docs/configuration-reference.md) if paths/commands change — reflect macOS install/run, audio routing (incl. virtual devices if recommended), and **feature matrix** (now-playing, ASCII video, Link, screen dump).
- **[AGENTS.md](../AGENTS.md)**: document zsh/bash equivalents for build/test/format where Windows PowerShell is assumed (keep Windows primary if desired, add macOS subsection).
- **Screen dump** ([ADR-0046](../docs/adr/0046-screen-dump-ascii-screenshot.md)): implement macOS provider **or** document Ctrl+Shift+E / CLI behavior when unavailable; avoid silent failure without messaging if ADR requires UX.
- **Console resizing**: review `HeaderContainer` / fullscreen paths for macOS terminal behavior; document “best terminal” guidance if BufferWidth APIs are no-ops.
- **Specs**: update [specs/console-ui/spec.md](../specs/console-ui/spec.md) Context (and any screen-dump scenario) per actual behavior.

**Out of scope**

- Pixel-perfect UI parity across Terminal.app vs iTerm vs Kitty — document only if blocking bugs found.

## Context pointer

- Primary spec: [`specs/platform-macos/spec.md`](../specs/platform-macos/spec.md)
- Depends on: [`PBI-012`](./PBI-012-macos-platform-foundation.md); coordinate with [`PBI-013`](./PBI-013-macos-audio-input.md) for audio docs.

## Verification pointer

- Spec **Definition of Done** (docs + degradations).
- Gherkin **graceful degradation** scenario passes for each Windows-only surface we expose.

## Acceptance criteria

- README / getting-started / product-audience accurately describe macOS support and limits.
- Screen dump and other OS-specific surfaces match documented behavior.

## Refinement rule

Any “macOS unsupported” claim in docs must match **runtime behavior** (hidden vs disabled vs error message).
