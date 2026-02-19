# ADR-0035: Modal dependency injection

**Status**: Accepted

## Context

Modals (DeviceSelectionModal, HelpModal, SettingsModal, ShowEditModal) were static classes with `Show(...)` methods receiving many parameters (6–8 each). This led to verbose call sites and made testing difficult. The DI pattern used for layers (ADR-0028) and visualizers (ADR-0008) was not applied to modals.

## Decision

1. **Modals are injectable services**. Each modal has an interface (`IDeviceSelectionModal`, `IHelpModal`, `ISettingsModal`, `IShowEditModal`) and an implementation class that receives dependencies via constructor injection.

2. **Modals are registered in the DI container** in ServiceConfiguration and resolved when needed. ApplicationShell receives modal services via constructor injection.

3. **Show methods accept only runtime parameters**. Dependencies (repositories, engine, visualizer settings, UI settings) are injected; call sites pass only what varies at runtime (e.g. `consoleLock`, `saveSettings`, `currentDeviceName`, `setModalOpen`).

4. **Initial device selection** (before ApplicationShell runs) uses `provider.GetRequiredService<IDeviceSelectionModal>().Show(...)` so the same modal implementation is used consistently.

## Consequences

- ApplicationShell and Program.cs have simpler call sites; fewer parameters are passed to modal Show methods.
- Modals can be mocked in tests by providing alternative implementations via ServiceConfigurationOptions.
- Consistent with ADR-0028 (layer DI) and ADR-0008 (visualizer settings DI).
- Interfaces live in `Abstractions/`; implementations in `Console/` (e.g. DeviceSelectionModal, HelpModal, SettingsModal, ShowEditModal).
- References: [ADR-0028](0028-layer-dependency-injection.md), [ADR-0006](0006-modal-system.md).
