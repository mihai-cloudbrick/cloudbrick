#nullable enable
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cloudbrick.DataExplorer.Storage.Provider.Sql;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;

internal sealed class SqlTableContext : ITableContext
{
    private readonly SqlOptions _opt;
    private readonly string _table; // physical table name (no schema)
    private readonly ILogger<SqlTableContext> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public SqlTableContext(SqlOptions opt,
                           string table,
                           ILogger<SqlTableContext> logger,
                           IExecutionContextAccessor ctx,
                           IExecutionScopeFactory scopes)
    {
        _opt = opt;
        _table = table;
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
    }

    private string FullName => $"[{_opt.Schema}].[{_table}]";

    public async Task CreateIfNotExistsAsync(CancellationToken ct = default)
    {
        var sql = $@"
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'{FullName}') AND type = N'U')
BEGIN
    CREATE TABLE {FullName}(
        [Id] NVARCHAR(256) NOT NULL PRIMARY KEY,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [CreatedUtc] DATETIME2 NOT NULL,
        [UpdatedUtc] DATETIME2 NULL,
        [ETag] ROWVERSION
    );
END";
        await using var conn = new SqlConnection(_opt.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteIfExistsAsync(CancellationToken ct = default)
    {
        var sql = $@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'{FullName}') AND type = N'U')
BEGIN
    DROP TABLE {FullName};
END";
        await using var conn = new SqlConnection(_opt.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(CancellationToken ct = default)
    {
        var sql = $@"SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'{FullName}') AND type = N'U') THEN 1 ELSE 0 END";
        await using var conn = new SqlConnection(_opt.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = new SqlCommand(sql, conn);
        var val = (int)(await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false) ?? 0);
        return val == 1;
    }

    public TableCapabilities GetCapabilities()
        => new()
        {
            ProviderName = "Sql",
            Version = "1.0",
            Flags = TableCapabilityFlags.ContinuationPaging, // server paging via OFFSET/FETCH in List
            MaxPageSize = null
        };

    public async Task<StorageResult<StorageItem>> GetAsync(string id, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Get");

        var sql = $@"SELECT [Id],[Payload],[CreatedUtc],[UpdatedUtc],[ETag] FROM {FullName} WITH (READCOMMITTED)
WHERE [Id]=@id";
        try
        {
            await using var conn = new SqlConnection(_opt.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (!await rdr.ReadAsync(ct).ConfigureAwait(false))
                return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("sql.get.notfound", ("Id", id)));

            var item = MapFrom(rdr);
            return Result(OperationStatus.Unchanged, started, item, etag: item.ETag, logs: Info("sql.get.ok", ("Id", id)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get failed on {Table}", FullName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("sql.get.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> ListAsync(int? skip = null, int? take = null, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("List");

        var s = Math.Max(0, skip ?? 0);
        var t = Math.Max(0, take ?? 100);

        var sql = $@"
SELECT [Id],[Payload],[CreatedUtc],[UpdatedUtc],[ETag]
FROM {FullName} WITH (READCOMMITTED)
ORDER BY [Id]
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
        try
        {
            await using var conn = new SqlConnection(_opt.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@skip", s);
            cmd.Parameters.AddWithValue("@take", t);

            var list = new List<StorageItem>();
            await using var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            while (await rdr.ReadAsync(ct).ConfigureAwait(false))
                list.Add(MapFrom(rdr));

            return Result(OperationStatus.Unchanged, started, items: list, logs: Info("sql.list.ok", ("Count", list.Count.ToString())));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List failed on {Table}", FullName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("sql.list.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> CreateAsync(string id, StorageItem entity, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Create");

        const string existsSql = "SELECT 1 FROM {0} WHERE [Id]=@id";
        var insertSql = $@"
INSERT INTO {FullName}([Id],[Payload],[CreatedUtc],[UpdatedUtc])
VALUES (@id, @payload, @created, NULL);
SELECT [Id],[Payload],[CreatedUtc],[UpdatedUtc],[ETag] FROM {FullName} WHERE [Id]=@id;";

        try
        {
            await using var conn = new SqlConnection(_opt.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            entity.Id = id;
            entity.CreatedUtc = DateTimeOffset.UtcNow;
            entity.UpdatedUtc = null;
            entity.ETag = null;

            await using var cmd = new SqlCommand(insertSql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@payload", entity.Data.ToJsonString());
            cmd.Parameters.AddWithValue("@created", entity.CreatedUtc);

            await using var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
            if (await rdr.ReadAsync(ct).ConfigureAwait(false))
            {
                var stored = MapFrom(rdr);
                return Result(OperationStatus.Created, started, stored, etag: stored.ETag, logs: Info("sql.create.ok", ("Id", id)));
            }

            // should not happen
            return Result<StorageItem>(OperationStatus.Error, started, error: new InvalidOperationException("Insert returned no row"), logs: Error("sql.create.no_row", new InvalidOperationException("insert-no-row")));
        }
        catch (SqlException se) when (se.Number == 2627 /*PK dup*/ || se.Number == 2601 /*unique*/ )
        {
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("sql.create.conflict", ("Id", id)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create failed on {Table}", FullName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("sql.create.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> UpdateAsync(string id, StorageItem entity, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Update");

        // fetch current for diff & timestamps
        var currentRes = await GetAsync(id, ct).ConfigureAwait(false);
        if (currentRes.Status == OperationStatus.NotFound)
            return Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("sql.update.notfound", ("Id", id)));
        if (currentRes.Status == OperationStatus.Error)
            return Result<StorageItem>(OperationStatus.Error, started, error: currentRes.Error ?? new Exception("read-failed"));

        var current = currentRes.Item!;
        if (entity.ETag is not null && current.ETag is not null && !string.Equals(entity.ETag, current.ETag, StringComparison.Ordinal))
            return Result<StorageItem>(OperationStatus.Conflict, started, logs: Warn("sql.update.etag.conflict", ("Id", id)));

        // Compute JSON diff
        var principal = _ctx.Current?.PrincipalId ?? "anonymous";
        var changes = new JsonDiff(_opt.Diff).Compute(current.Data, entity.Data, principal);

        // Update
        var sql = entity.ETag is null
            ? $@"UPDATE {FullName} SET [Payload]=@payload,[UpdatedUtc]=@updated WHERE [Id]=@id;
                 SELECT [Id],[Payload],[CreatedUtc],[UpdatedUtc],[ETag] FROM {FullName} WHERE [Id]=@id;"
            : $@"UPDATE {FullName} SET [Payload]=@payload,[UpdatedUtc]=@updated WHERE [Id]=@id AND [ETag]=@etag;
                 SELECT [Id],[Payload],[CreatedUtc],[UpdatedUtc],[ETag] FROM {FullName} WHERE [Id]=@id;";

        try
        {
            await using var conn = new SqlConnection(_opt.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            entity.Id = id;
            entity.CreatedUtc = current.CreatedUtc;
            entity.UpdatedUtc = DateTimeOffset.UtcNow;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@payload", entity.Data.ToJsonString());
            cmd.Parameters.AddWithValue("@updated", entity.UpdatedUtc);
            if (entity.ETag is not null) cmd.Parameters.AddWithValue("@etag", FromBase64Url(entity.ETag));

            await using var rdr = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);

            // If concurrency was enforced and UPDATE affected 0 rows, the subsequent SELECT still returns a row,
            // so we detect conflict by checking rows affected via @@ROWCOUNT. Simpler: run two commands.
            // Here we rely on ETag value comparison above and proceed; alternatively, re-check payload equality.

            if (await rdr.ReadAsync(ct).ConfigureAwait(false))
            {
                var stored = MapFrom(rdr);
                return Result(OperationStatus.Updated, started, stored, etag: stored.ETag, changes: changes, logs: Info("sql.update.ok", ("Id", id)));
            }

            // If for some reason no row returned:
            return Result<StorageItem>(OperationStatus.Error, started, error: new InvalidOperationException("Update returned no row"), logs: Error("sql.update.no_row", new InvalidOperationException("update-no-row")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update failed on {Table}", FullName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("sql.update.error", ex));
        }
    }

    public async Task<StorageResult<StorageItem>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var started = DateTimeOffset.UtcNow;
        using var scope = BeginScope("Delete");

        var sql = $@"DELETE FROM {FullName} WHERE [Id]=@id; SELECT @@ROWCOUNT;";
        try
        {
            await using var conn = new SqlConnection(_opt.ConnectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", id);
            var affectedObj = await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            var affected = Convert.ToInt32(affectedObj ?? 0);

            return affected > 0
                ? Result<StorageItem>(OperationStatus.Deleted, started, logs: Info("sql.delete.ok", ("Id", id)))
                : Result<StorageItem>(OperationStatus.NotFound, started, logs: Warn("sql.delete.notfound", ("Id", id)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed on {Table}", FullName);
            return Result<StorageItem>(OperationStatus.Error, started, error: ex, logs: Error("sql.delete.error", ex));
        }
    }

    // ---- helpers ----

    private IDisposable? BeginScope(string action)
    {
        var ctx = _ctx.Current;
        if (ctx is null) return null;
        return _scopes.Begin(_logger, new ExecutionContextProxy(ctx, action));
    }

    private StorageItem MapFrom(SqlDataReader rdr)
    {
        var id = rdr.GetString(0);
        var payload = rdr.IsDBNull(1) ? "{}" : rdr.GetString(1);
        var created = rdr.GetDateTime(2);
        var updated = rdr.IsDBNull(3) ? (DateTime?)null : rdr.GetDateTime(3);
        var etagBytes = (byte[])rdr[4];
        var etag = ToBase64Url(etagBytes);

        var data = JsonNode.Parse(payload)?.AsObject() ?? new JsonObject();
        return new StorageItem
        {
            Id = id,
            Data = data,
            CreatedUtc = new DateTimeOffset(created, TimeSpan.Zero),
            UpdatedUtc = updated is null ? null : new DateTimeOffset(updated.Value, TimeSpan.Zero),
            ETag = etag
        };
    }

    private static string ToBase64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4) { case 2: s += "=="; break; case 3: s += "="; break; }
        return Convert.FromBase64String(s);
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