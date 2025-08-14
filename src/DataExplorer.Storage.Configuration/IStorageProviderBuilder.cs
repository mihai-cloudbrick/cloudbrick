#nullable enable
using Cloudbrick.DataExplorer.Storage.Configuration;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

using Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IStorageProviderBuilder
{
    StorageProviderKind Kind { get; }

    /// <summary>
    /// Build a provider for a database id using its strongly-typed options.
    /// Implementations should validate and cast <paramref name="options"/> to their concrete type.
    /// </summary>
    IStorageProvider Build(string databaseId, IProviderOptions options);
}
