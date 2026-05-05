# AudioAnalyzer.Infrastructure

Technical implementations for audio capture, persistence, and rendering. Connects the application to external systems.

**Contents**: `SyntheticAudioInput` (demo mode), persistence repositories, logging, native Link shim adapter — **not** OS-specific capture (see platform projects per ADR-0084).

**Dependencies**: Application, Domain