#nullable enable
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.Sql;

using Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class SqlStorageProvider : IStorageProvider
{
    private readonly SqlOptions _opt;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SqlStorageProvider> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public SqlStorageProvider(SqlOptions opt,
                              ILoggerFactory loggerFactory,
                              ILogger<SqlStorageProvider> logger,
                              IExecutionContextAccessor ctx,
                              IExecutionScopeFactory scopes)
    {
        _opt = opt;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
    }

    public IDatabaseContext GetDatabase(string databaseId)
        => new SqlDatabaseContext(_opt, _loggerFactory, _ctx, _scopes, databaseId);
}
