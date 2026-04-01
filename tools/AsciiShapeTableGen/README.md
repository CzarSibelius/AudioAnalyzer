# AsciiShapeTableGen

Console tool that rasterizes each character of the shape charset with SixLabors fonts, samples six staggered regions per cell (matching `AsciiCellSampling`), normalizes per dimension, and prints C# for `AsciiShapeTable.Generated.cs`.

**Run** (from repo root):

```powershell
dotnet run --project tools/AsciiShapeTableGen
```

Redirect stdout to overwrite `src/AudioAnalyzer.Visualizers/TextLayers/AsciiModel/AsciiShapeTable.Generated.cs` when changing the charset or sampling layout.

Requires a monospace **TTF** on the machine (tries Consolas, Cascadia Mono, Lucida Console on Windows, or common Linux paths).
