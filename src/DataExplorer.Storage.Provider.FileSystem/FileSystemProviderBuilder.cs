#nullable enable
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

#nullable enable
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;

public sealed class FileSystemProviderBuilder : IStorageProviderBuilder
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<FileSystemStorageProvider> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public FileSystemProviderBuilder(ILoggerFactory loggerFactory,
                                     ILogger<FileSystemStorageProvider> logger,
                                     IExecutionContextAccessor ctx,
                                     IExecutionScopeFactory scopes)
    {
        _loggerFactory = loggerFactory;
        _logger = logger;
        _ctx = ctx;
        _scopes = scopes;
    }

    public StorageProviderKind Kind => StorageProviderKind.LocalFileSystem;

    public IStorageProvider Build(string databaseId, IProviderOptions options)
    {
        if (options is not FileSystemOptions opt)
            throw new InvalidOperationException($"Options for '{databaseId}' must be {nameof(FileSystemOptions)}.");

        return new FileSystemStorageProvider(opt, _loggerFactory, _logger, _ctx, _scopes);
    }
}
