namespace AudioAnalyzer.Domain;

/// <summary>Video input kind for the ASCII video text layer. Additional kinds may be added without changing <see cref="TextLayerType"/>.</summary>
public enum AsciiVideoSourceKind
{
    /// <summary>Live camera (Windows implementation in Platform.Windows).</summary>
    Webcam,

    /// <summary>Reserved; not implemented yet.</summary>
    File
}
