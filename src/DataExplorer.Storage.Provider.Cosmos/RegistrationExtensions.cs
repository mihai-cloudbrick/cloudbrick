#nullable enable
using Cloudbrick.DataExplorer.Storage.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudbrick.DataExplorer.Storage.Provider.Cosmos;

public static class RegistrationExtensions
{
    public static IServiceCollection AddAzureCosmosDatabase(
        this IServiceCollection services,
        string databaseId,
        Action<CosmosOptions> configure)
    {
        return services.AddResourceManagerStorageDatabase(databaseId, () =>
        {
            var opt = new CosmosOptions { Endpoint = "", Key = "" };
            configure(opt);
            // sane defaults already in the record
            return opt;
        });
    }
}
