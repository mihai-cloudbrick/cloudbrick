#nullable enable
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.Cosmos;

using Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class CosmosStorageProvider : IStorageProvider
{
    private readonly CosmosClient _client;
    private readonly CosmosOptions _opt;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<CosmosStorageProvider> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public CosmosStorageProvider(
        CosmosClient client,
        CosmosOptions options,
        ILoggerFactory loggerFactory,
        ILogger<CosmosStorageProvider> logger,
        IExecutionContextAccessor executionContext,
        IExecutionScopeFactory scopes)
    {
        _client = client;
        _opt = options;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _ctx = executionContext;
        _scopes = scopes;
    }

    public IDatabaseContext GetDatabase(string databaseId)
        => new CosmosDatabaseContext(_client, _opt, _loggerFactory, _ctx, _scopes, databaseId);
}
