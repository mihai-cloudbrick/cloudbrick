#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed record TableInfo(
    string TableId,
    string? PhysicalName = null,
    long? ApproxItemCount = null
);