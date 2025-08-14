#nullable enable
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

#nullable enable
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

using Cloudbrick.DataExplorer.Storage.Abstractions;

internal sealed class FileSystemDatabaseContext : IDatabaseContext
{
    private readonly FileSystemOptions _opt;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;
    private readonly string _databaseId;

    public string RootPath { get; }

    public FileSystemDatabaseContext(FileSystemOptions options,
                                     ILoggerFactory loggerFactory,
                                     IExecutionContextAccessor ctx,
                                     IExecutionScopeFactory scopes,
                                     string databaseId)
    {
        _opt = options;
        _loggerFactory = loggerFactory;
        _ctx = ctx;
        _scopes = scopes;
        _databaseId = databaseId;
        RootPath = Path.Combine(_opt.Root, ShardedPathBuilder.Sanitize(databaseId));
    }

    public Task CreateIfNotExistsAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(RootPath);
        return Task.CompletedTask;
    }

    public Task DeleteIfExistsAsync(CancellationToken ct = default)
    {
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, recursive: true);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<TableInfo>> ListTablesAsync(CancellationToken ct = default)
    {
        var list = new List<TableInfo>();
        if (!Directory.Exists(RootPath)) return Task.FromResult<IReadOnlyList<TableInfo>>(list);

        foreach (var dir in Directory.EnumerateDirectories(RootPath))
        {
            ct.ThrowIfCancellationRequested();
            var tableId = Path.GetFileName(dir);
            long approx = 0;
            try { approx = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly).LongCount(); } catch { }
            list.Add(new TableInfo(tableId, dir, approx));
        }
        return Task.FromResult<IReadOnlyList<TableInfo>>(list);
    }

    public ITableContext Table(string tableId)
    {
        var tablePath = Path.Combine(RootPath, ShardedPathBuilder.Sanitize(tableId));
        return new FileSystemTableContext( _opt, tablePath, _databaseId, tableId,
                                          _loggerFactory.CreateLogger<FileSystemTableContext>(),
                                          _ctx, _scopes);
    }


}
