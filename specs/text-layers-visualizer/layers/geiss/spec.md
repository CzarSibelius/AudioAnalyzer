# Geiss (deprecated — use TextLayers)

## Blueprint

### Context

_See Architecture._

### Architecture

### Constraints

## Contract

### Definition of Done

- Layer renders per preset JSON and TextLayers snapshot contracts.

### Regression guardrails

- New visual content is a **text layer** (`TextLayerRendererBase`), not a new `IVisualizer` ([ADR-0014](../../../../docs/adr/0014-visualizers-as-layers.md)).
- Viewport rules: .cursor/rules/visualizers-viewport.mdc.

### Scenarios

```gherkin
Scenario: Layer draws when enabled
  Given the layer is present in the active preset with Enabled true
  When TextLayersVisualizer renders a frame
  Then the layer writes cells consistent with its settings and snapshot inputs
```
