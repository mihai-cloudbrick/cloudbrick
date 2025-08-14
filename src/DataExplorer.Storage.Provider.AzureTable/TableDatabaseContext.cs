#nullable enable
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureTable;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Text.RegularExpressions;

internal sealed class TableDatabaseContext : IDatabaseContext
{
    private readonly TableServiceClient _svc;
    private readonly TableOptions _opt;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public string DatabaseId { get; }
    public string Prefix { get; } // sanitized db id + separator (or empty)

    public TableDatabaseContext(TableServiceClient svc,
                                TableOptions opt,
                                ILoggerFactory loggerFactory,
                                IExecutionContextAccessor ctx,
                                IExecutionScopeFactory scopes,
                                string databaseId)
    {
        _svc = svc;
        _opt = opt;
        _loggerFactory = loggerFactory;
        _ctx = ctx;
        _scopes = scopes;

        DatabaseId = databaseId;
        Prefix = SanitizeTableSegment(_opt.UseDatabasePrefix ? $"{databaseId}{_opt.Separator}" : string.Empty);
    }

    public Task CreateIfNotExistsAsync(CancellationToken ct = default)
    {
        // Azure Tables has no "database" to create. No-op.
        return Task.CompletedTask;
    }

    public async Task DeleteIfExistsAsync(CancellationToken ct = default)
    {
        // Best-effort: delete all tables matching the prefix
        await foreach (var table in _svc.QueryAsync(cancellationToken: ct))
        {
            if (!string.IsNullOrEmpty(Prefix) && table.Name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                await _svc.DeleteTableAsync(table.Name, ct).ConfigureAwait(false);
        }
    }
    public async Task<IReadOnlyList<TableInfo>> ListTablesAsync(CancellationToken ct = default)
    {
        var list = new List<TableInfo>();

        // Azure.Data.Tables client returns AsyncPageable<TableItem>
        await foreach (var table in _svc.QueryAsync(maxPerPage: 200, cancellationToken: ct))
        {
            var physical = table.Name;

            if (_opt.UseDatabasePrefix)
            {
                if (!physical.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                    continue;

                var logical = physical.Substring(Prefix.Length);
                list.Add(new TableInfo(
                    TableId: logical,
                    PhysicalName: physical,
                    ApproxItemCount: null // counting requires a query; leave null
                ));
            }
            else
            {
                list.Add(new TableInfo(
                    TableId: physical,
                    PhysicalName: physical,
                    ApproxItemCount: null
                ));
            }
        }

        return list;
    }

    public ITableContext Table(string tableId)
    {
        var physical = $"{Prefix}{SanitizeTableSegment(tableId)}";
        var client = _svc.GetTableClient(physical);
        return new TableTableContext(_opt, client,
                                     _loggerFactory.CreateLogger<TableTableContext>(),
                                     _ctx, _scopes, physical);
    }

    private string SanitizeTableSegment(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Table name cannot be null or empty.", nameof(input));

        // Remove all non-alphanumeric characters
        string sanitized = Regex.Replace(input, @"[^A-Za-z0-9]", "");

        // Ensure it starts with a letter (prepend 't' if not)
        if (!Regex.IsMatch(sanitized, @"^[A-Za-z]"))
        {
            sanitized = "t" + sanitized;
        }

        // Enforce length constraints (min 3, max 63)
        if (sanitized.Length < 3)
        {
            sanitized = sanitized.PadRight(3, 'x'); // pad with 'x'
        }
        else if (sanitized.Length > 63)
        {
            sanitized = sanitized.Substring(0, 63);
        }


        return sanitized;
    }
}
