#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Cloudbrick.DataExplorer.Storage.Configuration;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;

public static class RegistrationExtensions
{
    public static IServiceCollection AddAzureBlobDatabase(
        this IServiceCollection services,
        string databaseId,
        Action<BlobOptions> configure)
    {
        return services.AddResourceManagerStorageDatabase(databaseId, () =>
        {
            var opt = new BlobOptions { ConnectionString = "" }; // placeholder; configure will set
            configure(opt);
            // ensure sane defaults
            opt.ShardDepth = opt.ShardDepth <= 0 ? 2 : opt.ShardDepth;
            opt.ShardWidth = opt.ShardWidth <= 0 ? 2 : opt.ShardWidth;
            return opt;
        });
    }
}
