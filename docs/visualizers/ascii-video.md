# ASCII video (text layer)

Live **webcam** video rendered as ASCII characters in the text-layer viewport. The layer type is **`AsciiVideo`** (`TextLayerType.AsciiVideo`). Capture is **Windows-only** in this implementation; other platforms use a no-op frame source (layer shows a placeholder).

## Description

Frames are produced by **`IAsciiVideoFrameSource`** (Application abstraction). The console host registers **`WindowsAsciiVideoFrameSource`** on Windows ([ADR-0074](../adr/0074-ascii-video-layer-and-frame-source.md)) and **`NullAsciiVideoFrameSource`** elsewhere. Capture runs **off the main thread**; each visualization frame the visualizer calls **`PrepareForFrame`** once with the **frontmost** enabled ASCII video layer’s settings (highest `ZOrder`), then each **`AsciiVideoLayer`** reads the **latest** frame only (no unbounded queue).

**Privacy:** The camera is used only while an enabled `AsciiVideo` layer is present in the active preset (and the OS may prompt for permission). When no such layer is active, **`PrepareForFrame(null)`** stops capture and releases the device.

## Snapshot usage

- **Not** passed through `AudioAnalysisSnapshot` / `VisualizationFrameContext`. The layer uses constructor-injected **`IAsciiVideoFrameSource`** only ([ADR-0028](../adr/0028-layer-dependency-injection.md)).

## Settings

Layer-specific settings live in **`Custom`** as **`AsciiVideoSettings`** (S modal reflection). Common `TextLayerSettings` apply (`Enabled`, `ZOrder`, `PaletteId`, `ColorIndex`, `RenderBounds`, etc.).

| Property | Meaning |
|----------|---------|
| **Source** (`SourceKind`) | `Webcam` (implemented on Windows) or `File` (reserved; layer shows *File source N/A*) |
| **Webcam device** | Zero-based index into enumerated video capture device groups. In the S modal the row shows **`index · display name`** when WinRT provides a name (`MediaFrameSourceGroup.DisplayName`; list cached ~30s) |
| **Max capture width / height** | Optional caps (`0` = no cap). When **either** cap is greater than zero, the implementation considers only formats whose width (and/or height) does not exceed the non-zero cap(s), then picks the **largest** resolution (most pixels) among those. Omitting a cap on one axis means that axis is not filtered |
| **Palette source** | Same as ASCII image: layer palette vs per-pixel frame colors (`AsciiImagePaletteSource`) |
| **Flip horizontal** | When true, mirrors the sampled frame left–right in the layer viewport |

## Key bindings

No layer-specific keys in the initial implementation. Use **S** to edit settings; **←/→** to set layer type to **AsciiVideo**.

## Viewport constraints

Inherits the same minimums as other text layers (see [text-layers.md](text-layers.md)). Mapping samples the converted ASCII grid across the layer’s width × height (letterboxed cell grid).

## Implementation notes

- **Visualizer**: `AsciiVideoLayer` in `TextLayers/AsciiVideo/`; **`AsciiRasterConverter`** (BGRA → `AsciiFrame`, shared luminance ramp with ASCII image).
- **State**: **`AsciiVideoState`** in `ITextLayerStateStore<AsciiVideoState>` caches the last converted frame by source **sequence** id and convert size/palette mode.
- **Windows**: `WindowsAsciiVideoFrameSource` in `Platform.Windows/AsciiVideo/` uses WinRT **`MediaCapture`** + **`MediaFrameReader`**, double-buffered latest BGRA frame. Frame source selection prefers **`MediaFrameSourceKind.Color`** with **`MediaStreamType.VideoPreview`**, then Color + **`VideoRecord`**, then any Color stream, then preview/record fallbacks (stable order by source id). Many drivers expose pixels only on **`VideoMediaFrame.Direct3DSurface`**; when **`SoftwareBitmap`** is null, frames are copied via **`SoftwareBitmap.CreateCopyFromSurfaceAsync`** on a **non-blocking async path** (no synchronous `.GetResult()` on the WinRT task in the frame callback). **`IsWebcamSessionActive`** is set only after **`MediaFrameReader.StartAsync`** succeeds; **`IsWebcamStarting`** is true while the device is still opening. **Placeholders**: *No camera* (no session), *Opening camera* (starting), *Waiting for video* (streaming but no frame yet); after ~8s without a frame, a second line suggests checking **Camera** privacy in Windows Settings. Failures are logged under **`WindowsAsciiVideoFrameSource`** (event ids 7650–7657) when application logging is enabled ([ADR-0076](../adr/0076-configurable-application-logging.md)). **`WindowsAsciiVideoDeviceCatalog`** enumerates the same groups for settings labels via **`IAsciiVideoDeviceCatalog`**.
- **Interop**: `MemoryBufferInterop` reads **`BitmapBuffer`** through COM **`Marshal.QueryInterface`** for **`IMemoryBufferByteAccess`** on **`CreateReference()`**; a direct cast from the WinRT projection can throw **`InvalidCastException`** (e.g. with CsWinRT `IInspectable` wrappers).
- **Tests**: **`FakeAsciiVideoFrameSource`** supplies solid-color frames without hardware.

### Troubleshooting

- **IR / “Infrared” camera entries** (e.g. Windows Hello) expose **luminance-only** streams, not full RGB. For normal color video, choose **Integrated Camera** or another group that is clearly the RGB webcam, not the IR sensor.
- **Field of view looks cropped or “zoomed”** depends on the driver: different output resolutions often use different crops. After setting **Max capture width** and/or **Max capture height**, the app uses the **largest** format within those limits; try values such as **1280×720** vs **640×480** to see which mode matches what you expect in the **Camera** app.
- **Wrong or green-tinted color** is often fixed or confirmed outside the app: update the camera driver, compare the same device in the **Windows Camera** app, and check OEM camera settings. The layer consumes **BGRA8** after WinRT conversion; persistent tint usually traces to the driver or stream choice.

### Deferred / limitations

- **`File`** (and streams) are not implemented; only **`Webcam`** on Windows produces frames.
- **Multiple `AsciiVideo` layers**: A single capture session is driven by the **frontmost** enabled layer’s device index and caps. Other enabled `AsciiVideo` layers **read the same latest frame**; they do not open separate cameras in this version. Coordinating multiple simultaneous cameras is left for a future change.

## Related

- [ADR-0074](../adr/0074-ascii-video-layer-and-frame-source.md), [ADR-0014](../adr/0014-visualizers-as-layers.md), [ascii-image.md](ascii-image.md) (palette and ASCII mapping patterns).
