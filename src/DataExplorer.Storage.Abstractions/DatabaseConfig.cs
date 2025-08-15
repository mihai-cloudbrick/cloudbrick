#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed record DatabaseConfig(
    string DatabaseId,
    StorageProviderKind Kind,
    IReadOnlyDictionary<string, string> Settings
);
