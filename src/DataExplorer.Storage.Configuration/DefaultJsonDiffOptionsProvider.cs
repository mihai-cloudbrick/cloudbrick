#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

/// <summary>
/// Resolves per-database JsonDiff.Options from the typed options in the registration; falls back to JsonDiff.Default.
/// </summary>
public sealed class DefaultJsonDiffOptionsProvider : IJsonDiffOptionsProvider
{
    private readonly IDatabaseConfigManager _configs;

    public DefaultJsonDiffOptionsProvider(IDatabaseConfigManager configs) => _configs = configs;

    public JsonDiffOptions GetForDatabase(string databaseId)
    {
        var reg = _configs.GetAsync(databaseId).GetAwaiter().GetResult();
        return reg?.Options.Diff ?? new JsonDiffOptions();
    }
}
