#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.Storage;


namespace Cloudbrick.Orleans.Persistance.DataExplorer
{
    public static class SiloBuilderExtensions
    {
        public static ISiloBuilder AddResourceManagerGrainStorage(this ISiloBuilder builder, string name, Action<DataExplorerOrleansStorageOptions>? configure = null)
        {
            if (configure != null) builder.Services.Configure(name, configure);
            builder.Services.AddOptions<DataExplorerOrleansStorageOptions>(name);
            builder.Services.AddKeyedSingleton<IGrainStorage>(name, (sp, n) =>
            {
                var mgr = sp.GetRequiredService<IConfigAwareStorageManager>();
                var opts = sp.GetRequiredService<IOptionsMonitor<DataExplorerOrleansStorageOptions>>();
                var log = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DataExplorerGrainStorage>>();
                return new DataExplorerGrainStorage(name, mgr, opts, log);
            });
            return builder;
        }
    }
}
