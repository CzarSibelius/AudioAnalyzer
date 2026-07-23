using AudioAnalyzer.Application.Abstractions;

namespace AudioAnalyzer.Application.Diagnostics;

/// <summary>
/// Cross-platform Ableton Link probe: reports <see cref="FeatureCapabilityIds.AbletonLink"/> available
/// when the managed <see cref="ILinkSession"/> loaded its native shim, otherwise unavailable.
/// </summary>
public sealed class AbletonLinkCapabilityProbe : IFeatureCapabilityProbe
{
    private readonly ILinkSession _linkSession;

    /// <summary>Initializes a new instance of the <see cref="AbletonLinkCapabilityProbe"/> class.</summary>
    /// <param name="linkSession">Managed Ableton Link session (native shim wrapper).</param>
    public AbletonLinkCapabilityProbe(ILinkSession linkSession)
    {
        _linkSession = linkSession ?? throw new ArgumentNullException(nameof(linkSession));
    }

    /// <inheritdoc />
    public IReadOnlyList<FeatureCapabilityStatus> Probe()
    {
        bool available = _linkSession.IsAvailable;
        return
        [
            new FeatureCapabilityStatus(
                FeatureCapabilityIds.AbletonLink,
                "Ableton Link",
                available ? FeatureAvailability.Available : FeatureAvailability.Unavailable,
                available ? "" : "native link_shim not loaded",
                FeatureCapabilityCategory.Integration)
        ];
    }
}
