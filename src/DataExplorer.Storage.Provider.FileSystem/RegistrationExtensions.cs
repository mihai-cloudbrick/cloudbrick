#nullable enable
using Cloudbrick.DataExplorer.Storage.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

public static class RegistrationExtensions
{
    /// <summary>Add a Local FileSystem database with strongly-typed options.</summary>
    public static IServiceCollection AddLocalFileSystemDatabase(
        this IServiceCollection services,
        string databaseId,
        Action<FileSystemOptions> configure)
    {
        return services.AddResourceManagerStorageDatabase(databaseId, () =>
        {
            var opt = new FileSystemOptions { Root = "" }; // placeholder; configure will set
            configure(opt);
            // provide sensible defaults if needed:
            opt.ShardDepth = opt.ShardDepth <= 0 ? 2 : opt.ShardDepth;
            opt.ShardWidth = opt.ShardWidth <= 0 ? 2 : opt.ShardWidth;
            return opt;
        });
    }
}
