using Cloudbrick.Components.Blades.Routing;
using Cloudbrick.Components.Blades.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudbrick.Components.Blades.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCloudbrickBlades(this IServiceCollection services, Action<IBladeRegistry>? register = null)
    {
        var reg = new BladeRegistry();
        register?.Invoke(reg);

        services.AddSingleton<IBladeRegistry>(reg);
        services.AddScoped<IBladeDirtyRegistry, BladeDirtyRegistry>();
        services.AddScoped<IBladeManager, BladeManager>();
        services.AddScoped<IBladeRouteSerializer, BladeRouteSerializer>();
        return services;
    }
}
