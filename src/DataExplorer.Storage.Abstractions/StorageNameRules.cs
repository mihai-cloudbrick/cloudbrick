#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Text.RegularExpressions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public static class StorageNameRules
{
    // conservative defaults: letters/digits/._- ; must start alphanumeric
    private static readonly Regex DbTableRx = new(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.Compiled);
    // item ids can be longer and more flexible, but avoid control chars
    private static readonly Regex ItemIdRx = new(@"^[\x20-\x7E]{1,512}$", RegexOptions.Compiled);

    public static void ValidateDatabaseId(string databaseId)
    {
        if (string.IsNullOrWhiteSpace(databaseId) || !DbTableRx.IsMatch(databaseId))
            throw new ArgumentException($"Invalid DatabaseId '{databaseId}'. Allowed: alnum + . _ - (max 64), must start alnum.");
    }

    public static void ValidateTableId(string tableId)
    {
        if (string.IsNullOrWhiteSpace(tableId) || !DbTableRx.IsMatch(tableId))
            throw new ArgumentException($"Invalid TableId '{tableId}'. Allowed: alnum + . _ - (max 64), must start alnum.");
    }

    public static void ValidateItemId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !ItemIdRx.IsMatch(id))
            throw new ArgumentException("Invalid item id. 1..512 printable ASCII characters are allowed.");
    }
}