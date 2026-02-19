# AudioAnalyzer.Console

Console host and composition root. Configures dependency injection, handles keyboard input, and runs the main audio analysis loop.

**Contents**: `Program.cs` (bootstrap, device selection, runs ApplicationShell), `ServiceConfiguration` (DI setup), `ApplicationShell` (main loop, key handling, device capture), `DeviceResolver` (device resolution from settings), `Abstractions/` (modal interfaces), modals (DeviceSelectionModal, HelpModal, SettingsModal, ShowEditModal) and ShowPlaybackController as injectable services per ADR-0035.

**Dependencies**: Application, Domain, Infrastructure, Visualizers
