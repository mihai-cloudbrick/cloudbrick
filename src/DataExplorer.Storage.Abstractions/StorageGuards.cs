#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Text;
using System.Text.Json;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public static class StorageGuards
{
    private static readonly JsonSerializerOptions NoIndent = new() { WriteIndented = false };

    public static void EnsureWithinLimits(StorageItem item, StorageLimits? limits)
    {
        if (limits is null) return;
        if (limits.MaxItemSizeBytes <= 0) return;

        var size = Encoding.UTF8.GetByteCount(item.Data?.ToJsonString(NoIndent) ?? "{}");
        if (size > limits.MaxItemSizeBytes)
            throw new InvalidOperationException($"Item exceeds size limit: {size:N0} bytes > {limits.MaxItemSizeBytes:N0} bytes.");
    }

    public static JsonDiffOptions ApplyDiffCaps(JsonDiffOptions options, StorageLimits? limits)
    {
        if (limits is null) return options;
        return new JsonDiffOptions
        {
            RootPath = options.RootPath,
            MaxChanges = Math.Min(options.MaxChanges, limits.MaxChanges),
            MaxDepth = Math.Min(options.MaxDepth, limits.MaxDepth),
            DiffArrays = options.DiffArrays,
            ArrayOrderKeys = options.ArrayOrderKeys,
            NormalizeArraysBeforeDiff = options.NormalizeArraysBeforeDiff,
            CaseInsensitivePropertyLookup = options.CaseInsensitivePropertyLookup,
            TreatStringsAsDateTimeWhenPossible = options.TreatStringsAsDateTimeWhenPossible,
            MaxArrayItems = Math.Min(options.MaxArrayItems, limits.MaxArrayItems),
            AllowPath = options.AllowPath,
            DenyPath = options.DenyPath,
            RedactPath = options.RedactPath,
            RedactValue = options.RedactValue
        };
    }
}
