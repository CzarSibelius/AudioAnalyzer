# ASCII Model (AsciiModel)

Renders **Wavefront OBJ** (`.obj`) meshes as shaded ASCII art in the layer viewport. Meshes are loaded from a folder path, **normalized** to a unit box, **rotated** (Y axis or combined XYZ), and **projected** with perspective; each terminal cell is filled using a z-buffered triangle rasterizer and Lambert-style shading.

**Shape mode** (default) follows the ideas in [ASCII characters are not pixels](https://alexharri.com/blog/ascii-rendering): six staggered samples per cell build a **6D lightness vector**, which is matched to the closest **character shape vector** (precomputed from a monospace glyph raster). **Vertex normals** are interpolated per sample for smoother shading than flat face normals. Optional **global contrast** on that vector sharpens boundaries between differently lit regions. **Legacy gradient** mode keeps the older behavior: one sample per cell, face normal only, mapping to the same ramp as the ASCII Image layer (` .:-=+*#%@`).

## Snapshot usage

- **SpeedMultiplier** (common layer) — scales rotation and zoom animation speed with `SpeedBurst` and beat reactions.
- **BeatFlashActive** / **BeatCount** — optional **SpeedBurst** and **Flash** (advance to next `.obj` file) via Custom `BeatReaction`.

## Settings

- **Preset / layer JSON**: `Custom` holds `AsciiModelSettings` (discovered via reflection in the S modal).
- **Render mode**: `RenderMode` — `Shape` (default) or `LegacyGradient`. Shape mode uses more work per cell (six depth tests and lighting samples).
- **Shape contrast**: `ShapeContrastExponent` — applies only in `Shape` mode; `1.0` disables. Values above `1.0` apply global contrast (normalize the sampling vector by its max component, raise to this power, scale back), which tends to sharpen edges between regions.
- **Lighting**: Lambert diffuse with an **ambient** floor so back faces are not fully black: `intensity = Ambient + (1 - Ambient) × max(dot(n, L), 0)`.
  - **Ambient** (`0`–`1`, default `0.2`) — minimum brightness; raise if sides look too dark.
  - **Lighting preset** (`LightingPreset`): **Classic** — original fixed direction (~ toward +X,+Y,+Z); **Headlight** — light from the viewer (+Z), so the facing side stays brighter while the model rotates; **Custom** — use **Light azimuth °** and **Light elevation °** (horizontal angle from +X toward +Y; elevation from the XY plane toward +Z). Applies to both **Shape** and **Legacy gradient** modes.
- **Model folder**: `ModelFolderPath` — directory containing `.obj` files (sorted by name). When empty, the effective folder is the **global default asset base** from General settings (`UiSettings.DefaultAssetFolderPath`), or **`AppContext.BaseDirectory`** when unset; relative paths combine with that base; absolute paths ignore it. Which file is shown is stored as **`SelectedModelFileName`** in `Custom` (file name only; when null, the first sorted file). **I** and **Flash** advance it for all AsciiModel layers; persisted when save runs after a handled key. Not shown in the S modal. The application ships **`models/sample/`** next to the executable (`cube.obj`, `tetrahedron.obj`); use `models\\sample` as a **relative** layer path from the global base, or an absolute path, to point at those files.
- **Rotation**: `RotationAxis` (Y turntable or Xyz combined), `RotationDirection` (Clockwise / CounterClockwise), `RotationSpeed` (base step before SpeedMultiplier).
- **Zoom**: `EnableZoom`, `ZoomMin`, `ZoomMax`, `ZoomSpeed`, `ZoomStyle` (same styles as AsciiImage: Sine, Breathe, PingPong).
- **Performance**: `MaxTriangles` — if the mesh exceeds this count, the layer shows a short placeholder instead of rendering.

## Key bindings

- **I** — Advance to the next `.obj` in the folder (when any AsciiImage or AsciiModel layer exists), same as AsciiImage.
- **S** (settings modal) — **Enter** on **Model folder** opens the text editor for the path (same pattern as AsciiImage **Image path**).

## Viewport constraints

- Projection uses the **layer draw region** (full visualizer viewport when `RenderBounds` is omitted; otherwise the pixel rectangle from **RenderBounds**), so the model is centered and scaled to that rectangle ([ADR-0058](../adr/0058-layer-render-bounds.md)).
- Minimum terminal size follows the global TextLayers minimums.
- **Compositing**: In **Shape** mode, only cells where the projected mesh covers at least one subsample are written; other cells are left unchanged so **lower-ZOrder** layers show through the empty area around the model (within **RenderBounds** / clip). **Legacy** mode already wrote only covered cells.

## Implementation notes

- **Parser**: `ObjFileParser` supports `v` vertex lines and `f` faces (triangles and quads; polygons fan-triangulated). Vertex indices may use `v/vt/vn`; only the position index is used. Lines are `v` / `f` followed by whitespace (space or tab). **Vertex normals** are computed (angle-weighted from adjacent faces) for smooth shading in shape mode. **`ParseFile(IFileSystem, path)`** reads bytes via the same `IFileSystem` registered in DI (so tests can use `MockFileSystem`).
- **Assets**: `FileBasedLayerAssetPaths.GetSortedObjPaths` enumerates `.obj` files through `IFileSystem.Directory`; `AsciiModelLayer` uses `IFileInfo` for cache invalidation.
- **Rendering**: `AsciiModelRasterizer` — `System.Numerics` rotation, perspective projection with horizontal **cell aspect** correction (~2×) so models are not stretched vertically in the terminal. **Shape mode** uses fixed cell sampling positions shared with `AsciiShapeTable` (`AsciiCellSampling`); the final pass skips `Set` when no geometry hit so lower layers remain visible outside the silhouette. Character shape vectors live in `AsciiShapeTable.Generated.cs`; regenerate with `dotnet run --project tools/AsciiShapeTableGen` (requires a monospace TTF on the machine, e.g. Consolas on Windows). Directional light comes from `AsciiModelLighting.GetLightDirection` (preset or custom angles); diffuse is combined with **Ambient** per sample.
- **State**: `AsciiModelState` in `ITextLayerStateStore<AsciiModelState>` — cached mesh + file identity (length + last write), `RotationAngle`, `ZoomPhase`.
- **Deferred**: Directional contrast using neighbor-cell “external” samples (see the article) is not implemented; global contrast only.
- **References**: [ADR-0014](../adr/0014-visualizers-as-layers.md), [ADR-0043](../adr/0043-textlayer-state-store.md), [ADR-0055](../adr/0055-layer-specific-beat-reaction.md).
