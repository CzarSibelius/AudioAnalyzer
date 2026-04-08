# Known bugs and TODOs

This list is for **maintainers and agents**: confirmed product issues and small follow-ups that are easy to lose in chat or code comments. Prefer fixing over growing the list; when an item is resolved, remove it (optionally note the commit or PR in the change that fixed it).

**Where else deferred work lives**

- Larger refactors and structured task lists: [docs/refactoring/README.md](../refactoring/README.md).
- Architecture and behavior decisions: [docs/adr/README.md](../adr/README.md).

## Known bugs

- **AsciiModel — human-shaped OBJ**: The human model does not render correctly in the AsciiModel layer. Implementation: [`AsciiModelLayer.cs`](../../src/AudioAnalyzer.Visualizers/TextLayers/AsciiModel/AsciiModelLayer.cs); spec: [ascii-model.md](../visualizers/ascii-model.md).

## TODOs

None yet.
