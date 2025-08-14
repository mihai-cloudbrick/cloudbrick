#nullable enable
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureTable;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;

public sealed class AzureTableProviderBuilder : IStorageProviderBuilder
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TableStorageProvider> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public AzureTableProviderBuilder(ILoggerFactory loggerFactory,
                                     ILogger<TableStorageProvider> logger,
                                     IExecutionContextAccessor ctx,
                                     IExecutionScopeFactory scopes)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
    }

    public StorageProviderKind Kind => StorageProviderKind.AzureTableStorage;

    public IStorageProvider Build(string databaseId, IProviderOptions options)
    {
        var opt = options as TableOptions
                  ?? throw new InvalidOperationException($"Options for '{databaseId}' must be {nameof(TableOptions)}.");

        var svc = new TableServiceClient(opt.ConnectionString);
        return new TableStorageProvider(svc, opt, _loggerFactory, _logger, _ctx, _scopes);
    }
}
