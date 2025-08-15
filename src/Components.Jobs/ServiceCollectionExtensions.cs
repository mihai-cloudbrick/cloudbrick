using System;
using Cloudbrick.Components.Jobs.Options;
using Cloudbrick.Components.Jobs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudbrick.Components.Jobs
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCloudbrickJobsComponents(
            this IServiceCollection services,
            Action<CloudbrickJobsOptions>? configure = null)
        {
            if (configure != null) services.Configure(configure);
            else services.Configure<CloudbrickJobsOptions>(_ => { });

            services.AddScoped<SignalRTelemetryClient>();
            services.AddHttpClient<IJobsBackend, HttpJobsBackend>();
            return services;
        }
    }
}
