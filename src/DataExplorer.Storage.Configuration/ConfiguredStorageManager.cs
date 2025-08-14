#nullable enable
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

using Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class ConfiguredStorageManager : IConfigAwareStorageManager
{
    private readonly IDatabaseConfigManager _configManager;
    private readonly IProviderFactory _factory;
    private readonly ILogger<ConfiguredStorageManager> _logger;

    private readonly ConcurrentDictionary<string, IStorageProvider> _providers =
        new(StringComparer.OrdinalIgnoreCase);

    public ConfiguredStorageManager(IDatabaseConfigManager configManager,
                                    IProviderFactory factory,
                                    ILogger<ConfiguredStorageManager> logger)
    {
        _configManager = configManager;
        _factory = factory;
        _logger = logger;
    }

    public IDatabaseConfigManager Config => _configManager;
    public IProviderFactory Providers => _factory;

    public async Task InitializeAsync(bool createStructuresIfMissing = true, CancellationToken ct = default)
    {
        var all = await _configManager.ListAsync(ct).ConfigureAwait(false);
        if (all.Count == 0)
        {
            _logger.LogInformation("No database registrations found.");
            return;
        }

        foreach (var reg in all)
        {
            ct.ThrowIfCancellationRequested();

            var provider = _providers.GetOrAdd(reg.DatabaseId,
                _ => _factory.Create(reg.DatabaseId, reg.Options));

            if (createStructuresIfMissing)
            {
                var db = provider.GetDatabase(reg.DatabaseId);
                await db.CreateIfNotExistsAsync(ct).ConfigureAwait(false);
            }
        }
    }

    public IDatabaseContext Database(string databaseId)
    {
        var provider = _providers.GetOrAdd(databaseId, id =>
        {
            var reg = _configManager.GetAsync(id).GetAwaiter().GetResult()
                      ?? throw new KeyNotFoundException($"No registration for id '{id}'.");
            return _factory.Create(reg.DatabaseId, reg.Options);
        });

        return provider.GetDatabase(databaseId);
    }

    public void Invalidate(string databaseId) => _providers.TryRemove(databaseId, out _);
    public void InvalidateAll() => _providers.Clear();
}
