using Cloudbrick.Orleans.Jobs.Abstractions;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Managers;
using Cloudbrick.Orleans.Jobs.Executors;
using Cloudbrick.Orleans.Jobs.Managers;
using Cloudbrick.Orleans.Jobs.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orleans.Hosting;
using Orleans.Serialization;

namespace Cloudbrick.Orleans.Jobs
{
    public static class JobsSiloBuilderExtensions
    {
        public static ISiloBuilder AddCloudbrickJobsInMemory(this ISiloBuilder builder)
        {
            builder.Services.AddCloudbrickJobsCore();

            builder.AddMemoryGrainStorageAsDefault();
            builder.AddMemoryStreams(StreamConstants.ProviderName);
            builder.AddMemoryGrainStorage(StreamConstants.ProviderName);
            builder.Services.AddSerializer(c=>c.AddJsonSerializer(isSupported: ccc=> true));
            return builder;
        }

        private static IServiceCollection AddCloudbrickJobsCore(this IServiceCollection services)
        {
            services.AddSignalR();
            services.TryAddSingleton<ITaskExecutor, DelayExecutor>();
            services.TryAddSingleton<ITaskExecutor, AdderExecutor>();
            services.TryAddSingleton<ITaskExecutorFactory, ExecutorFactory>();
            services.TryAddSingleton<ITelemetrySinkFactory, TelemetrySinkFactory>();
            services.TryAddSingleton<IJobsManager, JobsManager>();
            services.TryAddSingleton<IScheduledJobsManager, ScheduledJobsManager>();
            services.TryAddSingleton<JobsOrchestrator>();
            services.TryAddSingleton<TelemetrySyncService>();
            return services;
        }
        public static WebApplication MapCloudbrickJobsTelemetryHub(this WebApplication app)
        {
            app.MapHub<TelemetryHub>("/_hubs/telemetry");

            return app;
        }
    }
}
