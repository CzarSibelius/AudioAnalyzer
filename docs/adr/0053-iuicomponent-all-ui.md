# ADR-0053: Use IUiComponent for all UI (including modals)

**Status**: Accepted

## Context

ADR-0052 introduced the IUiComponent tree and IUiComponentRenderer for header and main content. Other UI (modals: Settings, Help, Device selection, Show edit) still use custom renderers (e.g. ISettingsModalRenderer) or direct `Action drawContent` in ModalSystem. We want a single pattern for all UI.

## Decision

1. **All UI is built from IUiComponent and rendered via IUiComponentRenderer.** Any new screen, region, or overlay content must be expressed as a tree of IUiComponent nodes and rendered by calling IUiComponentRenderer with an appropriate RenderContext. No new ad-hoc draw actions or custom "draw" interfaces for new UI.

2. **Modals are in scope.** Modal *content* (what is drawn inside the overlay) is UI and therefore follows the same rule: it is described as an IUiComponent tree. The modal *runner* (ModalSystem per ADR-0006) still owns overlay region, input capture, and callbacks. The modal's draw callback is implemented by: building a root component (e.g. CompositeComponent) for the overlay content, building a RenderContext for the overlay (e.g. StartRow = 0, MaxLines = overlayRowCount, Width = console width, plus any modal-specific data on context or component), and calling IUiComponentRenderer.Render(root, context). No change to ModalSystem's signature is required: it continues to accept `Action drawContent`; that action is what invokes the component renderer.

3. **Existing modals.** Current implementations (SettingsModalRenderer, HelpModal, DeviceSelectionModal, ShowEditModal) may remain as-is until migrated. New modal content and any new modals must use the component tree. Migration of existing modals is incremental (can be done in follow-up work).

## Consequences

- One abstraction for all UI: same tree shape and single renderer entry point for header, main content, and modal content.
- New leaf component types may be needed for modal-specific content (e.g. list + detail panels); they are registered in UiComponentRenderer like existing leaves. Existing leaves (e.g. LabeledRowComponent) can be reused where they fit.
- Modal-specific renderer interfaces (e.g. ISettingsModalRenderer) can be superseded over time by component-based rendering; no requirement to remove them in this ADR.
- ADR-0006 (modal system) is unchanged: lifecycle and input still go through ModalSystem; only the *rendering* of modal content aligns with IUiComponent.
- References: [ADR-0052](0052-ui-container-component-renderer.md), [ADR-0006](0006-modal-system.md), [IUiComponent](../../src/AudioAnalyzer.Application/Abstractions/IUiComponent.cs), [RenderContext](../../src/AudioAnalyzer.Application/Abstractions/RenderContext.cs), [ModalSystem](../../src/AudioAnalyzer.Console/Console/ModalSystem.cs).
