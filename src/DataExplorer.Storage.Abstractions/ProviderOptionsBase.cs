#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Base for strongly-typed provider options. Each provider's options type should inherit this and set <see cref="Kind"/>.
/// </summary>
public abstract class ProviderOptionsBase : IProviderOptions
{
    public abstract StorageProviderKind Kind { get; }
    public JsonDiffOptions? Diff { get; set; }
    public StorageLimits? Limits { get; set; }
    public RetryOptions? Retry { get; set; }
}
