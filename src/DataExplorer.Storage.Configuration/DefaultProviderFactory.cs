#nullable enable
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

using Cloudbrick.DataExplorer.Storage.Abstractions;


public sealed class DefaultProviderFactory : IProviderFactory
{
    private readonly ILogger<DefaultProviderFactory> _logger;
    private readonly IReadOnlyDictionary<StorageProviderKind, IStorageProviderBuilder> _builders;

    public DefaultProviderFactory(IEnumerable<IStorageProviderBuilder> builders,
                                  ILogger<DefaultProviderFactory> logger)
    {
        _logger = logger;
        _builders = builders.ToDictionary(b => b.Kind, b => b);
        if (_builders.Count == 0)
            _logger.LogWarning("No IStorageProviderBuilder implementations are registered.");
    }

    public IStorageProvider Create(string databaseId, IProviderOptions options)
    {
        if (!_builders.TryGetValue(options.Kind, out var builder))
            throw new NotSupportedException($"No builder is registered for kind '{options.Kind}'.");

        _logger.LogDebug("Creating storage provider for DatabaseId='{DatabaseId}', Kind='{Kind}'",
                         databaseId, options.Kind);

        return builder.Build(databaseId, options);
    }
}
