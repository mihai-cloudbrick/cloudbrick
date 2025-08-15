#nullable enable
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureTable;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;
using System.Text.RegularExpressions;

internal sealed class TableTableContext : ITableContext
{
    private readonly TableOptions _opt;
    private readonly TableClient _table;
    private readonly ILogger<TableTableContext> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;
    private readonly JsonSerializerOptions _json = JsonSerializerFactory.Create();

    // Physical table name (for logs)
    private readonly string _tableName;

    public TableTableContext(TableOptions opt,
                             TableClient table,
                             ILogger<TableTableContext> logger,
                             IExecutionContextAccessor ctx,
                             IExecutionScopeFactory scopes,
                             string tableName)
    {
        _opt = opt;
        _table = table;
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
        _tableName = tableName;
    }

    public async Task CreateIfNotExistsAsync(CancellationToken ct = default)
        => await _table.CreateIfNotExistsAsync(ct).ConfigureAwait(false);

    public async Task DeleteIfExistsAsync(CancellationToken ct = default)
        => await _table.DeleteAsync(ct).ConfigureAwait(false);

    public async Task<bool> ExistsAsync(CancellationToken ct = default)
    {
        try
        {
            await _table.CreateIfNotExistsAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public TableCapabilities GetCapabilities()
        => new()
        {
            ProviderName = "AzureTable",
            Version = "1.0",
            Flags = TableCapabilityFlags.ContinuationPaging | TableCapabilityFlags.ParameterizedQuery,
            MaxPageSize = 1000
        };

    public async Task<StorageResult<StorageItem>> GetAsync(string id, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Get");

        var (pk, rk) = KeysFor(id);

        try
        {
            var resp = await _table.GetEntityAsync<TableEntity>(pk, rk, cancellationToken: ct).ConfigureAwait(false);
            var item = MapFrom(resp.Value);
            item.ETag = resp.Value.ETag.ToString();
            return Result(OperationStatus.Unchanged, started, item, etag: item.ETag, logs: Info("table.get.ok", ("Table", _tableName)));
        }
        catch (RequestFailedException rfe) when (rfe.Status == 404)
        {
            return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("table.get.notfound", ("Table", _tableName)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get failed on {Table}", _tableName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("table.get.error", ex));
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

            await foreach (var page in _table.QueryAsync<TableEntity>(maxPerPage: 1000, cancellationToken: ct).AsPages())
            {
                foreach (var e in page.Values)
                {
                    if (skip is int s && skipped < s) { skipped++; continue; }
                    if (remaining <= 0) break;

                    var si = MapFrom(e);
                    si.ETag = e.ETag.ToString();
                    results.Add(si);
                    remaining--;
                }
                if (remaining <= 0) break;
            }

            return Result(OperationStatus.Unchanged, started, items: results, logs: Info("table.list.ok", ("Count", results.Count.ToString())));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List failed on {Table}", _tableName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("table.list.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> CreateAsync(string id, StorageItem entity, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Create");

        var (pk, rk) = KeysFor(id);
        var te = new TableEntity(pk, rk);

        entity.Id = id;
        entity.CreatedUtc = DateTimeOffset.UtcNow;
        entity.UpdatedUtc = null;
        entity.ETag = null; // server-supplied

        MapTo(te, entity);

        try
        {
            await _table.AddEntityAsync(te, ct).ConfigureAwait(false);
            // Need to re-fetch to get server ETag (AddEntity doesn't return it)
            var got = await _table.GetEntityAsync<TableEntity>(pk, rk, cancellationToken: ct).ConfigureAwait(false);
            var stored = MapFrom(got.Value);
            stored.ETag = got.Value.ETag.ToString();

            return Result(OperationStatus.Created, started, stored, etag: stored.ETag, logs: Info("table.create.ok", ("Table", _tableName)));
        }
        catch (RequestFailedException rfe) when (rfe.Status == 409)
        {
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("table.create.conflict", ("Table", _tableName)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create failed on {Table}", _tableName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("table.create.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> UpdateAsync(string id, StorageItem entity, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Update");

        var (pk, rk) = KeysFor(id);

        // Read current for diff & ETag check
        TableEntity? currentEntity = null;
        StorageItem? currentItem = null;

        try
        {
            var resp = await _table.GetEntityAsync<TableEntity>(pk, rk, cancellationToken: ct).ConfigureAwait(false);
            currentEntity = resp.Value;
            currentItem = MapFrom(currentEntity);
            currentItem.ETag = currentEntity.ETag.ToString();
        }
        catch (RequestFailedException rfe) when (rfe.Status == 404)
        {
            return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("table.update.notfound", ("Table", _tableName)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read current entity on {Table}", _tableName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("table.update.read.error", ex));
        }

        // ETag precondition
        if (entity.ETag is not null && currentItem?.ETag is not null && !string.Equals(entity.ETag, currentItem.ETag, StringComparison.Ordinal))
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("table.update.etag.conflict", ("Table", _tableName)));

        // Compute changes
        var principal = _ctx.Current?.PrincipalId ?? "anonymous";
        var changes = new JsonDiff(_opt.Diff).Compute(currentItem?.Data ?? new(), entity.Data, principal);

        // Map updates
        entity.Id = id;
        entity.CreatedUtc = currentItem?.CreatedUtc ?? DateTimeOffset.UtcNow;
        entity.UpdatedUtc = DateTimeOffset.UtcNow;
        entity.ETag = null; // new ETag will come from server

        var updated = new TableEntity(pk, rk);
        MapTo(updated, entity);

        try
        {
            var ifMatch = currentEntity!.ETag; // replace based on current server ETag
            await _table.UpdateEntityAsync(updated, ifMatch, TableUpdateMode.Replace, ct).ConfigureAwait(false);

            var got = await _table.GetEntityAsync<TableEntity>(pk, rk, cancellationToken: ct).ConfigureAwait(false);
            var stored = MapFrom(got.Value);
            stored.ETag = got.Value.ETag.ToString();

            return Result(OperationStatus.Updated, started, stored, etag: stored.ETag, changes: changes, logs: Info("table.update.ok", ("Table", _tableName)));
        }
        catch (RequestFailedException rfe) when (rfe.Status == 412)
        {
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("table.update.precondition.failed", ("Table", _tableName)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update failed on {Table}", _tableName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("table.update.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Delete");

        var (pk, rk) = KeysFor(id);

        try
        {
            await _table.DeleteEntityAsync(pk, rk, ETag.All, ct).ConfigureAwait(false);
            return Result<StorageItem>(OperationStatus.Deleted, started, logs: Info("table.delete.ok", ("Table", _tableName)));
        }
        catch (RequestFailedException rfe) when (rfe.Status == 404)
        {
            return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("table.delete.notfound", ("Table", _tableName)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed on {Table}", _tableName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("table.delete.error", ex));
        }
    }

    // ----- helpers -----

    private (string Pk, string Rk) KeysFor(string id)
    {
        var hash = Sha256Hex(id);
        var pkLen = Math.Clamp(_opt.PartitionPrefixLength, 0, hash.Length);
        var pk = pkLen > 0 ? hash[..pkLen] : "0";
        var rk = _opt.RowKeyIsOriginalId ? id : hash;
        return (pk, rk);
    }

    private StorageItem MapFrom(TableEntity e)
    {
        var payload = e.GetString("Payload") ?? "{}";
        var data = JsonNode.Parse(payload)?.AsObject() ?? new();
        return new StorageItem
        {
            Id = e.GetString("Id") ?? e.RowKey,
            Data = data,
            CreatedUtc = e.TryGetValue("CreatedUtc", out var c) && c is DateTimeOffset cdo ? cdo : DateTimeOffset.UtcNow,
            UpdatedUtc = e.TryGetValue("UpdatedUtc", out var u) && u is DateTimeOffset udo ? udo : null
        };
    }
    
    private void MapTo(TableEntity e, StorageItem item)
    {
        e["Id"] = item.Id;
        e["Payload"] = item.Data.ToJsonString();
        e["CreatedUtc"] = item.CreatedUtc;
        e["UpdatedUtc"] = item.UpdatedUtc ?? null;
    }

    private IDisposable? BeginScope(string action)
    {
        var ctx = _ctx.Current;
        if (ctx is null) return null;
        return _scopes.Begin(_logger, new ExecutionContextProxy(ctx, action));
    }

    private static string Sha256Hex(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
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
