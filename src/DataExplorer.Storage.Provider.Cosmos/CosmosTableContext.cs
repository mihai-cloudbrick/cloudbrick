#nullable enable
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cloudbrick.DataExplorer.Storage.Provider.Cosmos;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;

internal sealed class CosmosTableContext : ITableContext
{
    private readonly CosmosOptions _opt;
    private readonly Container _container;
    private readonly ILogger<CosmosTableContext> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;
    private readonly JsonSerializerOptions _json = JsonSerializerFactory.Create();
    private readonly string _containerId;
    private readonly string _partitionKeyPath;

    public CosmosTableContext(CosmosOptions opt,
                              Container container,
                              ILogger<CosmosTableContext> logger,
                              IExecutionContextAccessor ctx,
                              IExecutionScopeFactory scopes,
                              string containerId,
                              string partitionKeyPath)
    {
        _opt = opt;
        _container = container;
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
        _containerId = containerId;
        _partitionKeyPath = partitionKeyPath;
    }

    public async Task CreateIfNotExistsAsync(CancellationToken ct = default)
    {
        var db = _container.Database;
        await db.CreateContainerIfNotExistsAsync(
                new ContainerProperties(_container.Id, _partitionKeyPath),
                throughput: _opt.DefaultThroughput,
                cancellationToken: ct)
            .ConfigureAwait(false);
    }

    public async Task DeleteIfExistsAsync(CancellationToken ct = default)
    {
        try
        {
            await _container.DeleteContainerAsync(cancellationToken: ct).ConfigureAwait(false);
        }
        catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // no-op
        }
    }

    public async Task<bool> ExistsAsync(CancellationToken ct = default)
    {
        try
        {
            var props = await _container.ReadContainerAsync(cancellationToken: ct).ConfigureAwait(false);
            return props.StatusCode == System.Net.HttpStatusCode.OK;
        }
        catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public TableCapabilities GetCapabilities()
        => new()
        {
            ProviderName = "Cosmos",
            Version = "1.0",
            Flags = TableCapabilityFlags.ServerSideQuery | TableCapabilityFlags.ContinuationPaging |
                    TableCapabilityFlags.ParameterizedQuery | TableCapabilityFlags.ServerSideProjection,
            MaxPageSize = 1000
        };

    public async Task<StorageResult<StorageItem>> GetAsync(string id, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Get");

        try
        {
            var pk = PartitionKeyFor(id);
            var resp = await _container.ReadItemStreamAsync(id, pk, cancellationToken: ct).ConfigureAwait(false);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("cosmos.get.notfound", ("Container", _containerId)));

            var item = await JsonSerializer.DeserializeAsync<StorageItem>(resp.Content, _json, ct).ConfigureAwait(false);
            var etag = resp.Headers.ETag;
            if (item is not null) item.ETag = etag;

            return Result(OperationStatus.Unchanged, started, item, etag: etag, logs: Info("cosmos.get.ok", ("Container", _containerId)));
        }
        catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("cosmos.get.notfound", ("Container", _containerId)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get failed in {Container}", _containerId);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("cosmos.get.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> ListAsync(int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("List");

        try
        {
            var results = new List<StorageItem>();
            int skipped = 0;
            int remaining = take ?? int.MaxValue;

            var query = new QueryDefinition("SELECT * FROM c");
            var reqOpts = new QueryRequestOptions { MaxItemCount = Math.Min(remaining, 1000) };

            using var it = _container.GetItemQueryIterator<StorageItem>(query, requestOptions: reqOpts);
            while (it.HasMoreResults && remaining > 0)
            {
                foreach (var item in await it.ReadNextAsync(ct).ConfigureAwait(false))
                {
                    if (skip is int s && skipped < s) { skipped++; continue; }
                    if (remaining <= 0) break;

                    // ETag isn't part of the POCO; we could re-read headers via stream API if needed
                    // For simplicity, leave ETag null here; callers typically re-read specific items for concurrency
                    results.Add(item);
                    remaining--;
                }
            }

            return Result(OperationStatus.Unchanged, started, items: results, logs: Info("cosmos.list.ok", ("Count", results.Count.ToString())));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List failed in {Container}", _containerId);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("cosmos.list.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> CreateAsync(string id, StorageItem entity, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Create");

        try
        {
            entity.Id = id;
            entity.CreatedUtc = DateTimeOffset.UtcNow;
            entity.UpdatedUtc = null;
            entity.ETag = null; // server supplies _etag

            var resp = await _container.CreateItemAsync(entity, PartitionKeyFor(id), cancellationToken: ct).ConfigureAwait(false);
            var etag = resp.ETag;
            entity.ETag = etag;

            return Result(OperationStatus.Created, started, entity, etag: etag, logs: Info("cosmos.create.ok", ("Container", _containerId)));
        }
        catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("cosmos.create.conflict", ("Container", _containerId)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create failed in {Container}", _containerId);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("cosmos.create.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> UpdateAsync(string id, StorageItem entity, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Update");

        // Read current for diff & If-Match
        StorageItem? current = null;
        string? currentEtag = null;

        try
        {
            var resp = await _container.ReadItemStreamAsync(id, PartitionKeyFor(id), cancellationToken: ct).ConfigureAwait(false);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("cosmos.update.notfound", ("Container", _containerId)));

            current = await JsonSerializer.DeserializeAsync<StorageItem>(resp.Content, _json, ct).ConfigureAwait(false);
            currentEtag = resp.Headers.ETag;
        }
        catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("cosmos.update.notfound", ("Container", _containerId)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read current item in {Container}", _containerId);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("cosmos.update.read.error", ex));
        }

        // If-Match precondition when client supplies ETag
        if (entity.ETag is not null && currentEtag is not null && !string.Equals(entity.ETag, currentEtag, StringComparison.Ordinal))
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("cosmos.update.etag.conflict", ("Container", _containerId)));

        // Compute JSON diff
        var principal = _ctx.Current?.PrincipalId ?? "anonymous";
        var changes = new JsonDiff(_opt.Diff).Compute(current?.Data ?? new(), entity.Data, principal);

        // Prepare replacement
        entity.Id = id;
        entity.CreatedUtc = current?.CreatedUtc ?? DateTimeOffset.UtcNow;
        entity.UpdatedUtc = DateTimeOffset.UtcNow;
        entity.ETag = null; // will be replaced by server's new etag

        try
        {
            var opts = new ItemRequestOptions
            {
                IfMatchEtag = currentEtag // enforce match against current
            };

            var resp = await _container.ReplaceItemAsync(entity, id, PartitionKeyFor(id), opts, ct).ConfigureAwait(false);
            var newEtag = resp.ETag;
            entity.ETag = newEtag;

            return Result(OperationStatus.Updated, started, entity, etag: newEtag, changes: changes, logs: Info("cosmos.update.ok", ("Container", _containerId)));
        }
        catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.PreconditionFailed)
        {
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("cosmos.update.precondition.failed", ("Container", _containerId)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update failed in {Container}", _containerId);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("cosmos.update.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Delete");

        try
        {
            var resp = await _container.DeleteItemAsync<StorageItem>(id, PartitionKeyFor(id), cancellationToken: ct).ConfigureAwait(false);
            return resp.StatusCode == System.Net.HttpStatusCode.NoContent || resp.StatusCode == System.Net.HttpStatusCode.OK
                ? Result<StorageItem>(OperationStatus.Deleted, started, logs: Info("cosmos.delete.ok", ("Container", _containerId)))
                : Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("cosmos.delete.notfound", ("Container", _containerId)));
        }
        catch (CosmosException ce) when (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("cosmos.delete.notfound", ("Container", _containerId)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed in {Container}", _containerId);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("cosmos.delete.error", ex));
        }
    }

    // ---- helpers ----

    private PartitionKey PartitionKeyFor(string id)
        => _partitionKeyPath == "/id" ? new PartitionKey(id) : PartitionKey.None; // simple default; extend for custom PKs

    private IDisposable? BeginScope(string action)
    {
        var ctx = _ctx.Current;
        if (ctx is null) return null;
        return _scopes.Begin(_logger, new ExecutionContextProxy(ctx, action));
    }

    private static StorageResult<T> Result<T>(OperationStatus status, DateTimeOffset started, T? item = default,
                                              IReadOnlyList<T>? items = null,
                                              string? etag = null,
                                              Exception? error = null,
                                              IReadOnlyDictionary<string, ChangeRecord>? changes = null,
                                              params LogEntry[] logs)
        => new()
        {
            Status = status,
            Item = item,
            Items = items,
            ETag = etag,
            StartedAtUtc = started,
            Duration = DateTimeOffset.UtcNow - started,
            Error = error,
            Logs = logs?.ToArray() ?? Array.Empty<LogEntry>(),
            Changes = changes
        };

    private static LogEntry Info(string message, params (string Key, string Value)[] props)
        => new()
        {
            Level = "Info",
            Message = message,
            Properties = props?.ToDictionary(p => p.Key, p => p.Value)
        };

    private static LogEntry Warn(string message, params (string Key, string Value)[] props)
        => new()
        {
            Level = "Warn",
            Message = message,
            Properties = props?.ToDictionary(p => p.Key, p => p.Value)
        };

    private static LogEntry Error(string message, Exception ex, params (string Key, string Value)[] props)
        => new()
        {
            Level = "Error",
            Message = message,
            Exception = ex.Message,
            Properties = props?.ToDictionary(p => p.Key, p => p.Value)
        };

    private sealed record ExecutionContextProxy(IExecutionContext Inner, string Action) : IExecutionContext
    {
        public string ActionName => Action;
        public string TrackingId => Inner.TrackingId;
        public string PrincipalId => Inner.PrincipalId;
        public DateTimeOffset StartedAtUtc => Inner.StartedAtUtc;
    }

  
}
