#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable


namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IProviderOptions
{
    StorageProviderKind Kind { get; }
    /// <summary>Optional per-database diff settings; falls back to JsonDiff.Default if null.</summary>
    JsonDiffOptions? Diff { get; }
}
