#nullable enable
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;

public sealed class AzureBlobProviderBuilder : IStorageProviderBuilder
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<BlobStorageProvider> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public AzureBlobProviderBuilder(ILoggerFactory loggerFactory,
                                    ILogger<BlobStorageProvider> logger,
                                    IExecutionContextAccessor ctx,
                                    IExecutionScopeFactory scopes)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
    }

    public StorageProviderKind Kind => StorageProviderKind.AzureBlobStorage;

    public IStorageProvider Build(string databaseId, IProviderOptions options)
    {
        var opt = options as BlobOptions
                  ?? throw new InvalidOperationException($"Options for '{databaseId}' must be {nameof(BlobOptions)}.");

        var svc = new BlobServiceClient(opt.ConnectionString);
        return new BlobStorageProvider(svc, opt, _loggerFactory, _logger, _ctx, _scopes);
    }
}
