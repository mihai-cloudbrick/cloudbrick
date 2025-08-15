#nullable enable
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureTable;

using Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class TableStorageProvider : IStorageProvider
{
    private readonly TableServiceClient _svc;
    private readonly TableOptions _opt;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TableStorageProvider> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public TableStorageProvider(
        TableServiceClient serviceClient,
        TableOptions options,
        ILoggerFactory loggerFactory,
        ILogger<TableStorageProvider> logger,
        IExecutionContextAccessor executionContext,
        IExecutionScopeFactory scopes)
    {
        _svc = serviceClient;
        _opt = options;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _ctx = executionContext;
        _scopes = scopes;
    }

    public IDatabaseContext GetDatabase(string databaseId)
        => new TableDatabaseContext(_svc, _opt, _loggerFactory, _ctx, _scopes, databaseId);
}
