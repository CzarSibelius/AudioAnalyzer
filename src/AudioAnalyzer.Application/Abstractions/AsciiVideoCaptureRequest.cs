using AudioAnalyzer.Domain;

namespace AudioAnalyzer.Application.Abstractions;

/// <summary>Desired capture session for the current visualization frame. Built from the frontmost enabled ASCII video layer.</summary>
public sealed class AsciiVideoCaptureRequest
{
    /// <summary>Initializes a new instance of the <see cref="AsciiVideoCaptureRequest"/> class.</summary>
    public AsciiVideoCaptureRequest(
        AsciiVideoSourceKind sourceKind,
        int webcamDeviceIndex,
        int? maxCaptureWidth,
        int? maxCaptureHeight)
    {
        SourceKind = sourceKind;
        WebcamDeviceIndex = webcamDeviceIndex;
        MaxCaptureWidth = maxCaptureWidth;
        MaxCaptureHeight = maxCaptureHeight;
    }

    /// <summary>Which source to use.</summary>
    public AsciiVideoSourceKind SourceKind { get; }

    /// <summary>Zero-based index into the platform&apos;s enumerated video capture devices.</summary>
    public int WebcamDeviceIndex { get; }

    /// <summary>Optional maximum frame width before ASCII conversion (null = platform default).</summary>
    public int? MaxCaptureWidth { get; }

    /// <summary>Optional maximum frame height before ASCII conversion (null = platform default).</summary>
    public int? MaxCaptureHeight { get; }
}
