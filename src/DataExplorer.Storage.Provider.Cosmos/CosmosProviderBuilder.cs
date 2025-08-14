#nullable enable
using Cloudbrick.DataExplorer.Storage.Provider.Cosmos;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.Cosmos;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class CosmosProviderBuilder : IStorageProviderBuilder
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<CosmosStorageProvider> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public CosmosProviderBuilder(ILoggerFactory loggerFactory,
                                 ILogger<CosmosStorageProvider> logger,
                                 IExecutionContextAccessor ctx,
                                 IExecutionScopeFactory scopes)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
    }

    public StorageProviderKind Kind => StorageProviderKind.CosmosDb;

    public IStorageProvider Build(string databaseId, IProviderOptions options)
    {
        var opt = options as CosmosOptions
                  ?? throw new InvalidOperationException($"Options for '{databaseId}' must be {nameof(CosmosOptions)}.");

        var clientOpts = new CosmosClientOptions
        {
            ConnectionMode = ConnectionMode.Gateway,
            UseSystemTextJsonSerializerWithOptions = JsonSerializerFactory.Create()
        };

        // Optional consistency (best-effort parse, non-fatal if invalid)
        if (!string.IsNullOrWhiteSpace(opt.Consistency) &&
            Enum.TryParse(opt.Consistency, ignoreCase: true, out ConsistencyLevel level))
        {
            clientOpts.ConsistencyLevel = level;
        }

        var client = new CosmosClient(opt.Endpoint, opt.Key, clientOpts);
        return new CosmosStorageProvider(client, opt, _loggerFactory, _logger, _ctx, _scopes);
    }
}
