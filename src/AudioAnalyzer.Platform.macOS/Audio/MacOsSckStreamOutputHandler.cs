using CoreMedia;
using Foundation;
using ScreenCaptureKit;

namespace AudioAnalyzer.Platform.macOS.Audio;

/// <summary>Forwards <see cref="ISCStreamOutput.DidOutputSampleBuffer"/> to a managed callback.</summary>
internal sealed class MacOsSckStreamOutputHandler : NSObject, ISCStreamOutput
{
    private readonly Action<CMSampleBuffer> _onAudioSample;

    /// <summary>Initializes a new instance of the <see cref="MacOsSckStreamOutputHandler"/> class.</summary>
    public MacOsSckStreamOutputHandler(Action<CMSampleBuffer> onAudioSample)
    {
        _onAudioSample = onAudioSample ?? throw new ArgumentNullException(nameof(onAudioSample));
    }

    /// <inheritdoc />
    public void DidOutputSampleBuffer(SCStream stream, CMSampleBuffer sampleBuffer, SCStreamOutputType type)
    {
        if (type != SCStreamOutputType.Audio)
        {
            return;
        }

        _onAudioSample(sampleBuffer);
    }
}
