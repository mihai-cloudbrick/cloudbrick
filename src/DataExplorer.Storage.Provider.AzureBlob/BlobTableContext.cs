#nullable enable
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;

internal sealed class BlobTableContext : ITableContext
{
    private readonly BlobContainerClient _container;
    private readonly BlobOptions _opt;
    private readonly string _tablePrefix; // e.g., "Users"
    private readonly ILogger<BlobTableContext> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;
    private readonly JsonSerializerOptions _json = JsonSerializerFactory.Create();

    public BlobTableContext(BlobContainerClient container,
                            BlobOptions options,
                            string tableId,
                            ILogger<BlobTableContext> logger,
                            IExecutionContextAccessor ctx,
                            IExecutionScopeFactory scopes)
    {
        _container = container;
        _opt = options;
        _tablePrefix = SanitizeSegment(tableId);
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
    }

    public async Task CreateIfNotExistsAsync(CancellationToken ct = default)
    {
        await _container.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);
        // Write a .keep blob to mark the table prefix
        var client = _container.GetBlobClient($"{_tablePrefix}/.keep");
        await client.UploadAsync(BinaryData.FromString(string.Empty), overwrite: true, cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task DeleteIfExistsAsync(CancellationToken ct = default)
    {
        // Delete all blobs under the prefix
        await foreach (var blob in _container.GetBlobsAsync(prefix: _tablePrefix + "/", cancellationToken: ct))
        {
            ct.ThrowIfCancellationRequested();
            await _container.DeleteBlobIfExistsAsync(blob.Name, cancellationToken: ct).ConfigureAwait(false);
        }
    }

    public async Task<bool> ExistsAsync(CancellationToken ct = default)
    {
        var client = _container.GetBlobClient($"{_tablePrefix}/.keep");
        var exists = await client.ExistsAsync(ct).ConfigureAwait(false);
        return exists.Value;
    }

    public TableCapabilities GetCapabilities()
        => new()
        {
            ProviderName = "AzureBlob",
            Version = "1.0",
            Flags = TableCapabilityFlags.ContinuationPaging, // query is client-side only
            MaxPageSize = null
        };

    public async Task<StorageResult<StorageItem>> GetAsync(string id, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Get");

        var name = BlobPathFor(id);
        var blob = _container.GetBlobClient(name);

        try
        {
            if (!await blob.ExistsAsync(ct).ConfigureAwait(false))
                return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("blob.get.notfound", ("Name", name)));

            var resp = await blob.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
            await using var s = resp.Value.Content;
            var item = await JsonSerializer.DeserializeAsync<StorageItem>(s, _json, ct).ConfigureAwait(false);
            var etag = resp.Value.Details.ETag.ToString();
            if (item is not null) item.ETag = etag;

            return Result(OperationStatus.Unchanged, started, item, etag: etag, logs: Info("blob.get.ok", ("Name", name)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get failed for {Name}", name);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("blob.get.error", ex, ("Name", name)));
        }
    }

    public async Task<StorageResult<StorageItem>> ListAsync(int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("List");

        var results = new List<StorageItem>();
        int skipped = 0;
        int remaining = take ?? int.MaxValue;

        try
        {
            await foreach (var item in _container.GetBlobsAsync(prefix: _tablePrefix + "/", cancellationToken: ct))
            {
                if (!item.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (skip is int s && skipped < s)
                {
                    skipped++;
                    continue;
                }
                if (remaining <= 0) break;

                var blob = _container.GetBlobClient(item.Name);
                var resp = await blob.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
                await using var streamResp = resp.Value.Content;
                var si = await JsonSerializer.DeserializeAsync<StorageItem>(streamResp, _json, ct).ConfigureAwait(false);
                if (si is not null)
                {
                    si.ETag = resp.Value.Details.ETag.ToString();
                    results.Add(si);
                    remaining--;
                }
            }

            return Result(OperationStatus.Unchanged, started, items: results, logs: Info("blob.list.ok", ("Count", results.Count.ToString())));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List failed under prefix {Prefix}", _tablePrefix);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("blob.list.error", ex, ("Prefix", _tablePrefix)));
        }
    }

    public async Task<StorageResult<StorageItem>> CreateAsync(string id, StorageItem entity, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Create");

        var name = BlobPathFor(id);
        var blob = _container.GetBlobClient(name);

        try
        {
            // Fail if exists (If-None-Match: *)
            using var ms = new MemoryStream();
            entity.Id = id;
            entity.CreatedUtc = DateTimeOffset.UtcNow;
            entity.UpdatedUtc = null;
            // ETag comes from server; clear any incoming client value
            entity.ETag = null;

            await JsonSerializer.SerializeAsync(ms, entity, _json, ct).ConfigureAwait(false);
            ms.Position = 0;

            var opts = new BlobUploadOptions
            {
                Conditions = new BlobRequestConditions { IfNoneMatch = new ETag("*") },
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
            };

            var resp = await blob.UploadAsync(ms, opts, ct).ConfigureAwait(false);
            var etag = resp.Value.ETag.ToString();

            entity.ETag = etag;
            return Result(OperationStatus.Created, started, entity, etag: etag, logs: Info("blob.create.ok", ("Name", name)));
        }
        catch (RequestFailedException rfe) when (rfe.Status == 412 || rfe.Status == 409)
        {
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("blob.create.conflict", ("Name", name)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create failed for {Name}", name);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("blob.create.error", ex, ("Name", name)));
        }
    }

    public async Task<StorageResult<StorageItem>> UpdateAsync(string id, StorageItem entity, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Update");

        var name = BlobPathFor(id);
        var blob = _container.GetBlobClient(name);

        // Read current for diff & optional ETag check
        StorageItem? current = null;
        string? currentEtag = null;

        try
        {
            var exists = await blob.ExistsAsync(ct).ConfigureAwait(false);
            if (!exists.Value)
                return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("blob.update.notfound", ("Name", name)));

            var read = await blob.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
            await using (var s = read.Value.Content)
                current = await JsonSerializer.DeserializeAsync<StorageItem>(s, _json, ct).ConfigureAwait(false);

            currentEtag = read.Value.Details.ETag.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read current blob {Name}", name);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("blob.update.read.error", ex, ("Name", name)));
        }

        // If-Match precondition (client provided ETag)
        if (entity.ETag is not null && currentEtag is not null && !string.Equals(entity.ETag, currentEtag, StringComparison.Ordinal))
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("blob.update.etag.conflict", ("Name", name)));

        // Compute changes
        var principal = _ctx.Current?.PrincipalId ?? "anonymous";
        var changes = new JsonDiff(_opt.Diff).Compute(current?.Data ?? new(), entity.Data, principal);

        // Prepare upload with If-Match (when provided)
        entity.Id = id;
        entity.CreatedUtc = current?.CreatedUtc ?? DateTimeOffset.UtcNow;
        entity.UpdatedUtc = DateTimeOffset.UtcNow;
        // ETag will be replaced by server's new ETag
        entity.ETag = null;


        try
        {
            using var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, entity, _json, ct).ConfigureAwait(false);
            ms.Position = 0;

            var opts = new BlobUploadOptions
            {
                Conditions = entity.ETag is null && currentEtag is not null
                    ? new BlobRequestConditions { IfMatch = new ETag(currentEtag) }
                    : null,
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
            };

            // Upload with overwrite
            var resp = await blob.UploadAsync(ms, opts, ct).ConfigureAwait(false);
            var etag = resp.Value.ETag.ToString();

            entity.ETag = etag;
            return Result(OperationStatus.Updated, started, entity, etag: etag, changes: changes, logs: Info("blob.update.ok", ("Name", name)));
        }
        catch (RequestFailedException rfe) when (rfe.Status == 412)
        {
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("blob.update.precondition.failed", ("Name", name)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update failed for {Name}", name);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("blob.update.error", ex, ("Name", name)));
        }
    }

    public async Task<StorageResult<StorageItem>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Delete");

        var name = BlobPathFor(id);
        try
        {
            var resp = await _container.DeleteBlobIfExistsAsync(name, DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: ct).ConfigureAwait(false);
            return resp.Value
                ? Result<StorageItem>(OperationStatus.Deleted, started, logs: Info("blob.delete.ok", ("Name", name)))
                : Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("blob.delete.notfound", ("Name", name)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed for {Name}", name);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("blob.delete.error", ex, ("Name", name)));
        }
    }

    // Helpers

    private string BlobPathFor(string id)
    {
        var hash = Sha256Hex(id);
        var parts = new List<string> { _tablePrefix };
        int idx = 0;
        for (int level = 0; level < _opt.ShardDepth; level++)
        {
            var take = Math.Min(_opt.ShardWidth, hash.Length - idx);
            if (take <= 0) break;
            parts.Add(hash.Substring(idx, take));
            idx += take;
        }
        parts.Add(hash + ".json");
        return string.Join("/", parts);
    }

    private static string SanitizeSegment(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "_";
        // Avoid slashes and control chars
        var cleaned = s.Replace('/', '_').Replace('\\', '_');
        return cleaned.Trim();
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
