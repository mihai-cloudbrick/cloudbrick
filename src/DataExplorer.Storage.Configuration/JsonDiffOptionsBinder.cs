#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

/// <summary>
/// Binds <see cref="JsonDiff.Options"/> to and from <see cref="DatabaseConfig.Settings"/> using DatabaseConfigKeys.*.
/// </summary>
public static class JsonDiffOptionsBinder
{
    public static JsonDiffOptions FromSettings(IReadOnlyDictionary<string, string> settings)
    {
        var d = new JsonDiffOptions();

        if (Try(settings, DatabaseConfigKeys.DiffRootPath, out var root) && !string.IsNullOrWhiteSpace(root))
            d = d with { RootPath = root };

        if (TryInt(settings, DatabaseConfigKeys.DiffMaxChanges, out var maxCh) && maxCh > 0)
            d = d with { MaxChanges = maxCh };

        if (TryInt(settings, DatabaseConfigKeys.DiffMaxDepth, out var maxD) && maxD > 0)
            d = d with { MaxDepth = maxD };

        if (Try(settings, DatabaseConfigKeys.DiffArrayOrderKeys, out var keys) && !string.IsNullOrWhiteSpace(keys))
            d = d with { ArrayOrderKeys = keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) };

        if (TryBool(settings, DatabaseConfigKeys.DiffNormalizeArrays, out var norm))
            d = d with { NormalizeArraysBeforeDiff = norm };

        if (TryBool(settings, DatabaseConfigKeys.DiffDiffArrays, out var diff))
            d = d with { DiffArrays = diff };

        if (TryBool(settings, DatabaseConfigKeys.DiffCaseInsensitivePropertyLookup, out var ci))
            d = d with { CaseInsensitivePropertyLookup = ci };

        if (TryBool(settings, DatabaseConfigKeys.DiffTreatStringsAsDateTime, out var dts))
            d = d with { TreatStringsAsDateTimeWhenPossible = dts };

        if (TryInt(settings, DatabaseConfigKeys.DiffMaxArrayItems, out var maxAi) && maxAi > 0)
            d = d with { MaxArrayItems = maxAi };

        return d;
    }

    public static void WriteToSettings(IDictionary<string, string> settings, JsonDiffOptions options)
    {
        settings[DatabaseConfigKeys.DiffRootPath] = options.RootPath;
        settings[DatabaseConfigKeys.DiffMaxChanges] = options.MaxChanges.ToString();
        settings[DatabaseConfigKeys.DiffMaxDepth] = options.MaxDepth.ToString();
        settings[DatabaseConfigKeys.DiffArrayOrderKeys] = string.Join(',', options.ArrayOrderKeys);
        settings[DatabaseConfigKeys.DiffNormalizeArrays] = options.NormalizeArraysBeforeDiff.ToString();
        settings[DatabaseConfigKeys.DiffDiffArrays] = options.DiffArrays.ToString();
        settings[DatabaseConfigKeys.DiffCaseInsensitivePropertyLookup] = options.CaseInsensitivePropertyLookup.ToString();
        settings[DatabaseConfigKeys.DiffTreatStringsAsDateTime] = options.TreatStringsAsDateTimeWhenPossible.ToString();
        settings[DatabaseConfigKeys.DiffMaxArrayItems] = options.MaxArrayItems.ToString();
    }

    private static bool Try(IReadOnlyDictionary<string, string> s, string k, out string v) => s.TryGetValue(k, out v);
    private static bool TryInt(IReadOnlyDictionary<string, string> s, string k, out int v)
    {
        v = 0;
        return s.TryGetValue(k, out var raw) && int.TryParse(raw, out v);
    }
    private static bool TryBool(IReadOnlyDictionary<string, string> s, string k, out bool v)
    {
        v = false;
        return s.TryGetValue(k, out var raw) && bool.TryParse(raw, out v);
    }
}
