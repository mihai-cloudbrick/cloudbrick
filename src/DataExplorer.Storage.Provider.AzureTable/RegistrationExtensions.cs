#nullable enable
using Cloudbrick.DataExplorer.Storage.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureTable;

public static class RegistrationExtensions
{
    public static IServiceCollection AddAzureTableDatabase(
        this IServiceCollection services,
        string databaseId,
        Action<TableOptions> configure)
    {
        return services.AddResourceManagerStorageDatabase(databaseId, () =>
        {
            var opt = new TableOptions { ConnectionString = "" };
            configure(opt);
            return opt;
        });
    }
}
