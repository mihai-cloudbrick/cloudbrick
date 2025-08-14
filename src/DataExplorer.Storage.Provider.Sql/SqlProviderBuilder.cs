#nullable enable
using Cloudbrick.DataExplorer.Storage.Provider.Sql;

#nullable enable
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.Sql;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;

public sealed class SqlProviderBuilder : IStorageProviderBuilder
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SqlStorageProvider> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public SqlProviderBuilder(ILoggerFactory loggerFactory,
                              ILogger<SqlStorageProvider> logger,
                              IExecutionContextAccessor ctx,
                              IExecutionScopeFactory scopes)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
    }

    public StorageProviderKind Kind => StorageProviderKind.SqlDatabase;

    public IStorageProvider Build(string databaseId, IProviderOptions options)
    {
        var opt = options as SqlOptions
                  ?? throw new InvalidOperationException($"Options for '{databaseId}' must be {nameof(SqlOptions)}.");

        return new SqlStorageProvider(opt, _loggerFactory, _logger, _ctx, _scopes);
    }
}
