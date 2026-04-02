# AsciiShapeTableGen — folder layout

**Location**: `tools/AsciiShapeTableGen/`

**Purpose**: standalone console tool (ImageSharp) that generates raster data for the AsciiModel text layer.

**Layout**: typically a single `Program.cs` and the `.csproj`. Keep the tool self-contained; do not reference production projects unless there is a deliberate shared contract.

**Output**: generated C# such as `AsciiShapeTable.Generated.cs` lives under **`src/AudioAnalyzer.Visualizers/TextLayers/AsciiModel/`** and is committed after regeneration. Document any regen command in the tool’s README or in [docs/visualizers/ascii-model.md](../../visualizers/ascii-model.md) if that file exists.

## Rules

- Prefer `net10.0` to match the solution unless the tool requires otherwise.
- New build-time tools: add under `tools/<ToolName>/` and reference this pattern in `docs/agents/project-structure/AGENTS.md` if they become permanent.
