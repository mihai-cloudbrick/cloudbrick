#nullable enable
using Cloudbrick.DataExplorer.Storage.Provider.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.Sql;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Data;

internal sealed class SqlDatabaseContext : IDatabaseContext
{
    private readonly SqlOptions _opt;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public string DatabaseId { get; }
    public string Schema => _opt.Schema;

    public SqlDatabaseContext(SqlOptions opt,
                              ILoggerFactory loggerFactory,
                              IExecutionContextAccessor ctx,
                              IExecutionScopeFactory scopes,
                              string databaseId)
    {
        _opt = opt;
        _loggerFactory = loggerFactory;
        _ctx = ctx;
        _scopes = scopes;
        DatabaseId = databaseId;
    }

    public async Task CreateIfNotExistsAsync(CancellationToken ct = default)
    {
        // Ensure schema exists (DB itself is assumed to exist)
        const string sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = @schema)
BEGIN
    DECLARE @ddl nvarchar(4000) = N'CREATE SCHEMA [' + @schema + N']';
    EXEC sp_executesql @ddl;
END";
        await using var conn = new SqlConnection(_opt.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", _opt.Schema);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public Task DeleteIfExistsAsync(CancellationToken ct = default)
    {
        // We do NOT drop the database. Table-level deletion is exposed at the table context.
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<TableInfo>> ListTablesAsync(CancellationToken ct = default)
    {
        var list = new List<TableInfo>();
        var schema = _opt.Schema ?? "dbo";

            const string sql = @"
                                SELECT t.name AS TableName
                                FROM sys.tables t
                                JOIN sys.schemas s ON s.schema_id = t.schema_id
                                WHERE s.name = @schema
                                ORDER BY t.name;";

        await using var conn = new SqlConnection(_opt.ConnectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add(new SqlParameter("@schema", SqlDbType.NVarChar, 128) { Value = schema });

        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();
            var name = reader.GetString(0);
            list.Add(new TableInfo(name, $"{schema}.{name}", null));
        }

        return list;
    }

    public ITableContext Table(string tableId)
    {
        var physical = SanitizeTableName(tableId);
        return new SqlTableContext(_opt, physical,
                                   _loggerFactory.CreateLogger<SqlTableContext>(),
                                   _ctx, _scopes);
    }

    private static string SanitizeTableName(string name)
    {
        // Allow letters, digits, underscore; replace others with '_'
        var cleaned = new string(name.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray());
        if (string.IsNullOrWhiteSpace(cleaned)) cleaned = "items";
        return cleaned;
    }
}
