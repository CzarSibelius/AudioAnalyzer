using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Infrastructure.Link;

/// <summary>
/// Ableton Link session via native <c>link_shim.dll</c>. When the library is missing, <see cref="IsAvailable"/> is false.
/// </summary>
public sealed class LinkSessionNative : ILinkSession
{
    private IntPtr _handle;
    private bool _disposed;
    private bool _enabled;

    /// <summary>Creates a session; failures leave <see cref="IsAvailable"/> false.</summary>
    public LinkSessionNative()
    {
        try
        {
            _handle = LinkShimNative.link_shim_create(120.0);
        }
        catch (DllNotFoundException)
        {
            _handle = IntPtr.Zero;
        }
        catch (EntryPointNotFoundException)
        {
            _handle = IntPtr.Zero;
        }
    }

    /// <inheritdoc />
    public bool IsAvailable => _handle != IntPtr.Zero;

    /// <inheritdoc />
    public bool IsEnabled => _enabled;

    /// <inheritdoc />
    public void SetEnabled(bool enabled)
    {
        if (_handle == IntPtr.Zero)
        {
            return;
        }

        _enabled = enabled;
        LinkShimNative.link_shim_set_enabled(_handle, enabled ? 1 : 0);
    }

    /// <inheritdoc />
    public void Capture(out double tempoBpm, out int numPeers, out double beat, double quantum)
    {
        tempoBpm = 0;
        numPeers = 0;
        beat = 0;
        if (_handle == IntPtr.Zero)
        {
            return;
        }

        _ = LinkShimNative.link_shim_capture(_handle, quantum, out tempoBpm, out numPeers, out beat);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_handle != IntPtr.Zero)
        {
            LinkShimNative.link_shim_destroy(_handle);
            _handle = IntPtr.Zero;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
