#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed record StorageLimits
{
    /// <summary>Max UTF-8 size of the JSON payload (default 512 KB). 0 or negative disables.</summary>
    public int MaxItemSizeBytes { get; init; } = 512 * 1024;

    /// <summary>Hard cap for diff entries (defaults harmonize with JsonDiff.Options).</summary>
    public int MaxChanges { get; init; } = 512;

    /// <summary>Hard cap for diff recursion depth.</summary>
    public int MaxDepth { get; init; } = 8;

    /// <summary>Hard cap for array items visited when DiffArrays=true.</summary>
    public int MaxArrayItems { get; init; } = 256;
}