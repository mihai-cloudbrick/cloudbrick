#nullable enable
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;

using Cloudbrick.DataExplorer.Storage.Abstractions;

internal sealed class BlobDatabaseContext : IDatabaseContext
{
    private readonly BlobServiceClient _svc;
    private readonly BlobOptions _opt;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IExecutionContextAccessor _ctx;
    private readonly IExecutionScopeFactory _scopes;

    public string ContainerName { get; }

    public BlobDatabaseContext(BlobServiceClient svc,
                               BlobOptions opt,
                               ILoggerFactory loggerFactory,
                               IExecutionContextAccessor ctx,
                               IExecutionScopeFactory scopes,
                               string databaseId)
    {
        _svc = svc;
        _opt = opt;
        _loggerFactory = loggerFactory;
        _ctx = ctx;
        _scopes = scopes;
        ContainerName = SanitizeContainerName(databaseId);
    }

    public async Task CreateIfNotExistsAsync(CancellationToken ct = default)
    {
        var container = _svc.GetBlobContainerClient(ContainerName);
        await container.CreateIfNotExistsAsync(cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task DeleteIfExistsAsync(CancellationToken ct = default)
    {
        var container = _svc.GetBlobContainerClient(ContainerName);
        await container.DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<TableInfo>> ListTablesAsync(CancellationToken ct = default)
    {
        var list = new List<TableInfo>();

        var container = _svc.GetBlobContainerClient(ContainerName);

        // If container doesn't exist, return empty list quietly.
        try
        {
            var exists = await container.ExistsAsync(ct).ConfigureAwait(false);
            if (!exists.Value) return list;
        }
        catch (RequestFailedException)
        {
            return list;
        }

        // List top-level "folders" (prefixes). Each prefix = a logical table id.
        await foreach (var page in container.GetBlobsByHierarchyAsync(delimiter: "/")
                                            .AsPages(pageSizeHint: 500)
                                            .WithCancellation(ct))
        {
            foreach (var item in page.Values)
            {
                if (!item.IsPrefix) continue;
                var prefix = item.Prefix?.TrimEnd('/') ?? string.Empty;
                if (string.IsNullOrWhiteSpace(prefix)) continue;

                list.Add(new TableInfo(
                    TableId: prefix,
                    PhysicalName: $"{ContainerName}/{prefix}",
                    ApproxItemCount: null // estimating blob counts per prefix is expensive; omit by default
                ));
            }
        }

        return list;
    }

    // Minimal, conservative sanitizer for container naming rules.
    private static string SanitizeContainerName(string name)
    {
        // lower-case, letters/digits/hyphen only, 3-63 chars
        var lower = name.Trim().ToLowerInvariant();
        var sb = new System.Text.StringBuilder(lower.Length);
        foreach (var ch in lower)
        {
            if (ch >= 'a' && ch <= 'z' || ch >= '0' && ch <= '9' || ch == '-') sb.Append(ch);
            else if (ch == '.' || ch == '_') sb.Append('-');
        }
        var s = sb.ToString().Trim('-');
        if (s.Length < 3) s = s.PadRight(3, '0');
        if (s.Length > 63) s = s.Substring(0, 63);
        return s;
    }

    public ITableContext Table(string tableId)
    {
        var container = _svc.GetBlobContainerClient(ContainerName);
        return new BlobTableContext(container, _opt, tableId,
                                    _loggerFactory.CreateLogger<BlobTableContext>(),
                                    _ctx, _scopes);
    }
}
