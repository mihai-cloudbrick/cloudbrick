#nullable enable
using Cloudbrick.DataExplorer.Storage.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

using Cloudbrick.DataExplorer.Storage.Abstractions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddResourceManagerStorageCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IDatabaseConfigManager, InMemoryDatabaseConfigManager>();
        services.TryAddSingleton<IProviderFactory, DefaultProviderFactory>();

        services.TryAddSingleton<DefaultExecutionContextAccessor>();
        services.TryAddSingleton<IExecutionContextAccessor>(sp => sp.GetRequiredService<DefaultExecutionContextAccessor>());
        services.TryAddSingleton<IExecutionScopeFactory, DefaultExecutionScopeFactory>();

        services.TryAddSingleton<IJsonDiffOptionsProvider, DefaultJsonDiffOptionsProvider>();

        services.TryAddSingleton<ConfiguredStorageManager>();
        services.TryAddSingleton<IConfigAwareStorageManager>(sp => sp.GetRequiredService<ConfiguredStorageManager>());
        services.TryAddSingleton<IStorageManager>(sp => sp.GetRequiredService<ConfiguredStorageManager>());

        return services;
    }

    /// <summary>Add or update a typed registration for a single database.</summary>
    public static IServiceCollection AddResourceManagerStorageDatabase<TOptions>(
        this IServiceCollection services,
        string databaseId,
        Func<TOptions> createOptions)
        where TOptions : class, IProviderOptions
    {
        var opt = createOptions();

        var database = new DatabaseRegistration(databaseId, opt);
        services.AddSingleton(database);
        return services;
    }
}
