# ADR-0004: Visualizer encapsulation — keep visualizer logic inside visualizers

**Status**: Accepted

## Context

The renderer, console, and application layer need to drive multiple visualizers (Spectrum, Oscilloscope, Geiss, Unknown Pleasures, etc.) without the codebase turning into a web of mode-specific branches. If callers depend on concrete visualizer types or on implementation details (e.g. that "Geiss" has beat circles, or that "Unknown Pleasures" uses a palette), adding or changing a visualizer forces edits in many places and leaks visualizer internals.

## Decision

1. **Contain visualizer-specific logic in the visualizer**: All logic that is specific to how a given visualizer works (e.g. how it uses palette, beat reaction, gain, or any internal state) must live inside that visualizer's implementation as much as possible. Shared contracts (e.g. `IVisualizer`, `AnalysisSnapshot`, viewport) define the boundary; visualizers consume the snapshot and viewport and do not expose internal behavior beyond what the interface requires.

2. **Other code must not depend on visualizer internals**: The renderer, console, and application must not reference concrete visualizer types (e.g. `GeissVisualizer`) for behavior that could be expressed via the shared interface or via the snapshot. They must not branch on visualizer identity for logic that belongs inside the visualizer. Prefer extending the snapshot or the `IVisualizer` contract (e.g. optional capabilities or well-named snapshot properties) so that callers stay mode-agnostic.

3. **Naming and data flow**: Snapshot and settings use mode-agnostic or capability-based naming where feasible (e.g. "palette for current mode" rather than visualizer-specific names in the long term). Data that visualizers need is passed in via the snapshot or viewport; visualizers do not rely on callers invoking visualizer-specific methods or properties.

## Consequences

- **Renderer** (`CompositeVisualizationRenderer`): Should not hold references to concrete visualizer types (e.g. `_geissVisualizer`) for features like beat circles; such behavior should be driven via snapshot or a generic mechanism so the renderer only uses `IVisualizer` and mode.
- **Application/Domain**: Snapshot and interfaces may gain capability-based or generic properties so that visualizer-specific options (e.g. "show beat circles", palette, gain) are supplied in a uniform way rather than via visualizer-specific APIs.
- **Visualizers**: Remain the single place that implement mode-specific behavior; they read from the snapshot and viewport and do not require callers to know their internals.
- **Documentation**: When adding or changing visualizers, agents and developers should prefer extending the shared contract and keeping logic inside the visualizer.
