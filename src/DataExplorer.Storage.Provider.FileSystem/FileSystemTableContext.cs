#nullable enable
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;

internal sealed class FileSystemTableContext : ITableContext
{
    private readonly FileSystemOptions _opt;
    private readonly string _tablePath;
    private readonly string _databaseId;
    private readonly string _tableId;
    private readonly ILogger<FileSystemTableContext> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;
    private readonly JsonSerializerOptions _json = JsonSerializerFactory.Create();

    public FileSystemTableContext(FileSystemOptions options,
                                  string tablePath,
                                  string databaseId, string tableId,
                                  ILogger<FileSystemTableContext> logger,
                                  IExecutionContextAccessor ctx,
                                  IExecutionScopeFactory scopes)
    {
        _opt = options;
        _tablePath = tablePath;
        _databaseId = databaseId;
        _tableId = tableId;
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
    }

    public Task CreateIfNotExistsAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(_tablePath);
        return Task.CompletedTask;
    }

    public Task DeleteIfExistsAsync(CancellationToken ct = default)
    {
        if (Directory.Exists(_tablePath))
            Directory.Delete(_tablePath, recursive: true);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(CancellationToken ct = default)
        => Task.FromResult(Directory.Exists(_tablePath));

    public TableCapabilities GetCapabilities()
        => new()
        {
            ProviderName = "FileSystem",
            Version = "1.0",
            Flags = TableCapabilityFlags.None, // query is client-side only and not exposed here
            MaxPageSize = null
        };
   
    public async Task<StorageResult<StorageItem>> GetAsync(string id, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Get");

        var file = ItemPath(id);
        if (!File.Exists(file))
        {
            return Result<StorageItem>(OperationStatus.NotFound, started, null, logs: Info("fs.get.notfound", ("Path", file)));
        }

        try
        {
            await using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
            var item = await JsonSerializer.DeserializeAsync<StorageItem>(fs, _json, ct).ConfigureAwait(false);
            return Result(OperationStatus.Unchanged, started, item, etag: item?.ETag, logs: Info("fs.get.ok", ("Path", file)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read item from {Path}", file);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("fs.get.error", ex, ("Path", file)));
        }
    }

    public async Task<StorageResult<StorageItem>> ListAsync(int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("List");

        try
        {
            var files = Directory.Exists(_tablePath)
                ? Directory.EnumerateFiles(_tablePath, "*.json", SearchOption.AllDirectories)
                : Enumerable.Empty<string>();

            if (skip is int s && s > 0) files = files.Skip(s);
            if (take is int t && t >= 0) files = files.Take(t);

            var result = new List<StorageItem>();
            foreach (var f in files)
            {
                ct.ThrowIfCancellationRequested();
                await using var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
                var item = await JsonSerializer.DeserializeAsync<StorageItem>(fs, _json, ct).ConfigureAwait(false);
                if (item is not null) result.Add(item);
            }

            return Result(OperationStatus.Unchanged, started, items: result, logs: Info("fs.list.ok", ("Count", result.Count.ToString())));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List failed in {Table}", _tablePath);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("fs.list.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> CreateAsync(string id, StorageItem entity, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Create");

        var file = ItemPath(id);

        if (File.Exists(file))
        {
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("fs.create.conflict", ("Path", file)));
        }

        try
        {
            ShardedPathBuilder.EnsureParentDirectory(file);
            entity.Id = id;
            entity.CreatedUtc = DateTimeOffset.UtcNow;
            entity.UpdatedUtc = null;
            entity.ETag = NewEtag();

            var tmp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await using (var fs = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
                await JsonSerializer.SerializeAsync(fs, entity, _json, ct).ConfigureAwait(false);

#if NET9_0_OR_GREATER
            File.Move(tmp, file, overwrite: false);
#else
            File.Move(tmp, file);
#endif
            return Result(OperationStatus.Created, started, entity, etag: entity.ETag, logs: Info("fs.create.ok", ("Path", file)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create failed for {Path}", file);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("fs.create.error", ex, ("Path", file)));
        }
    }

    public async Task<StorageResult<StorageItem>> UpdateAsync(string id, StorageItem entity, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Update");

        var file = ItemPath(id);
        if (!File.Exists(file))
            return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("fs.update.notfound", ("Path", file)));

        StorageItem? current = null;
        try
        {
            await using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true))
                current = await JsonSerializer.DeserializeAsync<StorageItem>(fs, _json, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read current item {Path}", file);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("fs.update.read.error", ex, ("Path", file)));
        }

        // Concurrency check
        if (entity.ETag is not null && current?.ETag is not null && !string.Equals(entity.ETag, current.ETag, StringComparison.Ordinal))
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("fs.update.etag.conflict", ("Path", file)));

        // Compute changes
        var principal = _ctx.Current?.PrincipalId ?? "anonymous";
        var changes = new JsonDiff(_opt.Diff).Compute(current?.Data ?? new(), entity.Data, principal);

        // Update fields
        entity.Id = id;
        entity.CreatedUtc = current?.CreatedUtc ?? DateTimeOffset.UtcNow;
        entity.UpdatedUtc = DateTimeOffset.UtcNow;
        entity.ETag = NewEtag();

        try
        {
            var tmp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            await using (var w = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, useAsync: true))
                await JsonSerializer.SerializeAsync(w, entity, _json, ct).ConfigureAwait(false);

#if NET9_0_OR_GREATER
            File.Move(tmp, file, overwrite: true);
#else
            File.Delete(file); File.Move(tmp, file);
#endif
            return Result(OperationStatus.Updated, started, entity, etag: entity.ETag, changes: changes, logs: Info("fs.update.ok", ("Path", file)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update failed for {Path}", file);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("fs.update.error", ex, ("Path", file)));
        }
    }

    public Task<StorageResult<StorageItem>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Delete");

        var file = ItemPath(id);
        if (!File.Exists(file))
            return Task.FromResult(Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("fs.delete.notfound", ("Path", file))));

        try
        {
            File.Delete(file);
            return Task.FromResult(Result<StorageItem>(OperationStatus.Deleted, started, logs: Info("fs.delete.ok", ("Path", file))));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed for {Path}", file);
            return Task.FromResult(Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("fs.delete.error", ex, ("Path", file))));
        }
    }

    // Helpers

    private string ItemPath(string id)
        => ShardedPathBuilder.BuildItemPath(_opt, Directory.GetParent(_tablePath)!.Parent!.FullName, // root
                                            Path.GetFileName(Directory.GetParent(_tablePath)!.FullName)!, // db
                                            Path.GetFileName(_tablePath)!, id);

    private IDisposable? BeginScope(string action)
    {
        var ctx = _ctx.Current;
        if (ctx is null) return null;
        var logger = (ILogger)_logger;
        return _scopes.Begin(logger, new ExecutionContextProxy(ctx, action));
    }

    private static string NewEtag() => Guid.NewGuid().ToString("N");

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
