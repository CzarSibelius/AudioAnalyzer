# Agent instructions (by topic)

This folder holds agent instructions grouped by topic. The root [AGENTS.md](../../AGENTS.md) contains the one-sentence project description, build/test commands, and links here.

| File | Purpose |
|------|--------|
| [csharp-and-static-analysis.md](csharp-and-static-analysis.md) | C# style, no empty catch, braces, XML docs, one file per class, linter and build |
| [documentation.md](documentation.md) | When to update README, configuration-reference.md, visualizer specs, UI specs, deferred work |
| [ui-and-console.md](ui-and-console.md) | UI specs, viewport, key handling, alignment, user controls |
| [visualizers.md](visualizers.md) | Viewport contract, new layers, layer settings, visualizer specs |
| [testing-and-verification.md](testing-and-verification.md) | Build/test/format checklist, screen dump, display and audio testing |
| [git-workflow.md](git-workflow.md) | Commit timing and messages |
| [architecture-overview.md](architecture-overview.md) | Reference: components, data flow, colors, debugging tips |
| [project-structure/AGENTS.md](project-structure/AGENTS.md) | **Where to put files** — per-project folder layout (agents must follow) |
| [native-link-shim-build.md](native-link-shim-build.md) | **Ableton Link** — CMake + MSVC Build Tools, Developer shell, clone `third_party/link`, build `link_shim.dll` |

**Configuration file reference** (presets, shows, palettes, `appsettings.json`, NuGet versions): [configuration-reference.md](../configuration-reference.md).

Canonical architecture decisions are in [docs/adr/](../adr/README.md). Cursor rules in `.cursor/rules/` (e.g. `adr.mdc`) apply on top of these docs.
