#nullable enable
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;

using Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class BlobStorageProvider : IStorageProvider
{
    private readonly BlobServiceClient _svc;
    private readonly BlobOptions _opt;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<BlobStorageProvider> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public BlobStorageProvider(
        BlobServiceClient serviceClient,
        BlobOptions options,
        ILoggerFactory loggerFactory,
        ILogger<BlobStorageProvider> logger,
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
        => new BlobDatabaseContext(_svc, _opt, _loggerFactory, _ctx, _scopes, databaseId);
}
