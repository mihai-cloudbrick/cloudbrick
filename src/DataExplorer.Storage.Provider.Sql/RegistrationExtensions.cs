#nullable enable
using Cloudbrick.DataExplorer.Storage.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudbrick.DataExplorer.Storage.Provider.Sql;

public static class RegistrationExtensions
{
    public static IServiceCollection AddSqlDatabase(
        this IServiceCollection services,
        string databaseId,
        Action<SqlOptions> configure)
    {
        return services.AddResourceManagerStorageDatabase(databaseId, () =>
        {
            var opt = new SqlOptions { ConnectionString = "" };
            configure(opt);
            if (string.IsNullOrWhiteSpace(opt.Schema)) opt.Schema = "dbo";
            return opt;
        });
    }
}
