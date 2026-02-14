# AudioAnalyzer.Console

Console host and composition root. Configures dependency injection, handles keyboard input, and runs the main audio analysis loop.

**Contents**: `Program.cs` (key handling, main loop), `ServiceConfiguration` (DI setup), `DeviceResolver` (device resolution from settings), `PaletteResolver` (palette resolution and application)

**Dependencies**: Application, Domain, Infrastructure, Visualizers
