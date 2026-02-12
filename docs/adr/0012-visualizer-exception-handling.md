# ADR-0012: Visualizer exception handling — show error in viewport

**Status**: Accepted

## Context

Visualizers can throw exceptions (null refs, bad data, edge cases). If exceptions propagate, the program crashes and the user sees no feedback. Implementing try-catch in every visualizer would duplicate logic and risk inconsistent behavior. A single, centralized policy is needed.

## Decision

1. **No crash on visualizer exception**: When `IVisualizer.Render` throws, the program must not crash.
2. **Show error in viewport**: The error message must be displayed in the visualizer viewport area (where the visualization would normally appear), using viewport bounds (start row, width, max lines).
3. **Centralized logic**: Exception handling is implemented once in the renderer (`CompositeVisualizationRenderer`). Individual visualizers do not add their own try-catch for this purpose.
4. **Message display**: Show the exception message (`ex.Message`) when available and non-empty, truncated to viewport width. Fallback to a generic label (e.g. "Visualization error") if the message is empty.

## Consequences

- **CompositeVisualizationRenderer**: Keeps the try-catch around `visualizer.Render()`; on catch, renders `ex.Message` (or fallback) truncated to `viewport.Width` at the visualizer start row.
- **Individual visualizers**: No requirement to add try-catch for rendering; they may throw and the renderer handles it.
- **Consistency with ADR-0011**: The catch block is not empty—it performs meaningful handling (display error in viewport).
- **HandleKey**: Out of scope for this ADR. Key handling has no viewport; could be a future ADR if needed.
