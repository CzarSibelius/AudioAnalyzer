using System.Reflection;
using AudioAnalyzer.Visualizers;
using Microsoft.Extensions.DependencyInjection;

namespace AudioAnalyzer.Console;

/// <summary>Extension methods for configuring text layer renderers in the DI container.</summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>Registers all text layer renderer implementations discovered via reflection. Requires INowPlayingProvider to be registered for NowPlayingLayer.</summary>
    public static IServiceCollection AddTextLayerRenderers(this IServiceCollection services)
    {
        var interfaceType = typeof(ITextLayerRenderer);
        var assembly = interfaceType.Assembly;

        var implementations = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && interfaceType.IsAssignableFrom(t));

        foreach (var impl in implementations)
        {
            services.AddSingleton(interfaceType, impl);
        }

        return services;
    }
}
