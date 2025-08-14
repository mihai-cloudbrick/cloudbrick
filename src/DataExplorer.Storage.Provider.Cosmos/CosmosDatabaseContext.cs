#nullable enable
using Cloudbrick.DataExplorer.Storage.Provider.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.Cosmos;

using Cloudbrick.DataExplorer.Storage.Abstractions;

internal sealed class CosmosDatabaseContext : IDatabaseContext
{
    private readonly CosmosClient _client;
    private readonly CosmosOptions _opt;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public string DatabaseId { get; }

    public CosmosDatabaseContext(CosmosClient client,
                                 CosmosOptions opt,
                                 ILoggerFactory loggerFactory,
                                 IExecutionContextAccessor ctx,
                                 IExecutionScopeFactory scopes,
                                 string databaseId)
    {
        _client = client;
        _opt = opt;
        _loggerFactory = loggerFactory;
        _ctx = ctx;
        _scopes = scopes;
        DatabaseId = SanitizeDatabaseId(databaseId);
    }

    public async Task CreateIfNotExistsAsync(CancellationToken ct = default)
        => await _client.CreateDatabaseIfNotExistsAsync(DatabaseId, cancellationToken: ct).ConfigureAwait(false);

    public async Task DeleteIfExistsAsync(CancellationToken ct = default)
    {
        try
        {
            var db = _client.GetDatabase(DatabaseId);
            await db.DeleteAsync(cancellationToken: ct).ConfigureAwait(false);
        }
        catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // no-op
        }
    }
    public async Task<IReadOnlyList<TableInfo>> ListTablesAsync(CancellationToken ct = default)
    {
        var list = new List<TableInfo>();

        var db = _client.GetDatabase(DatabaseId);

        // Check database existence quickly; swallow 404s and return empty.
        try
        {
            using var resp = await db.ReadStreamAsync(cancellationToken: ct).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return list;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return list;
        }

        using var iterator = db.GetContainerQueryIterator<ContainerProperties>(
            queryText: "SELECT c.id FROM c",
            requestOptions: new QueryRequestOptions { MaxItemCount = 100 });

        while (iterator.HasMoreResults)
        {
            ct.ThrowIfCancellationRequested();
            var page = await iterator.ReadNextAsync(ct).ConfigureAwait(false);
            foreach (var c in page)
            {
                var name = c.Id;
                list.Add(new TableInfo(
                    TableId: name,
                    PhysicalName: name,
                    ApproxItemCount: null // estimating counts needs RU; skip by default
                ));
            }
        }

        return list;
    }

    public ITableContext Table(string tableId)
    {
        var db = _client.GetDatabase(DatabaseId);
        var containerId = SanitizeContainerId(tableId);
        var container = db.GetContainer(containerId);
        return new CosmosTableContext(_opt, container, _loggerFactory.CreateLogger<CosmosTableContext>(), _ctx, _scopes, containerId, _opt.PartitionKeyPath);
    }

    private static string SanitizeDatabaseId(string id)
    {
        // Cosmos DB id rules: 1-255 chars; cannot contain '/', '\', '#', '?'
        var cleaned = new string(id.Select(ch => ch == '/' || ch == '\\' || ch == '#' || ch == '?' ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "db" : cleaned;
    }

    private static string SanitizeContainerId(string id)
    {
        var cleaned = new string(id.Select(ch => ch == '/' || ch == '\\' || ch == '#' || ch == '?' ? '-' : ch).ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "items" : cleaned;
    }
}
