#nullable enable
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

using Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class FileSystemStorageProvider : IStorageProvider
{
    private readonly FileSystemOptions _options;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<FileSystemStorageProvider> _logger;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public FileSystemStorageProvider(
        FileSystemOptions options,
        ILoggerFactory loggerFactory,
        ILogger<FileSystemStorageProvider> logger,
        IExecutionContextAccessor executionContext,
        IExecutionScopeFactory scopes)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _loggerFactory = loggerFactory;
        _logger = logger;
        _ctx = executionContext;
        _scopes = scopes;

        Directory.CreateDirectory(_options.Root);
    }

    public IDatabaseContext GetDatabase(string databaseId)
        => new FileSystemDatabaseContext(_options, _loggerFactory, _ctx, _scopes, databaseId);
}
