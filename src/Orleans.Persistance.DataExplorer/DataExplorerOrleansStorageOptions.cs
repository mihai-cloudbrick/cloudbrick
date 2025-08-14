#nullable enable
using Cloudbrick;
using System.Text.Json;

namespace Cloudbrick.Orleans.Persistance.DataExplorer;

public sealed class DataExplorerOrleansStorageOptions
{
    public string DefaultDatabaseId { get; set; } = "default-db";
    public string? DefaultTableId { get; set; }
    public Func<string, string, string?, (string DatabaseId, string TableId, string ItemId)> Mapper { get; set; } = DefaultMapper;
    public bool CreateStructuresIfMissing { get; set; } = true;
    public JsonSerializerOptions JsonOptions { get; set; } = new(JsonSerializerDefaults.Web);
    public static (string DatabaseId, string TableId, string ItemId) DefaultMapper(string grainType, string primaryKey, string? keyExt)
    {
        var table = StorageNameSanitizer.Sanitize(grainType);
        var id = string.IsNullOrEmpty(keyExt) ? primaryKey : $"{primaryKey}|{keyExt}";
        return ("default-db", table, id);
    }
}
internal static class StorageNameSanitizer
{
    public static string Sanitize(string s){ if(string.IsNullOrWhiteSpace(s)) return "t"; var ok=s.Select(ch=>char.IsLetterOrDigit(ch)||ch=='.'||ch=='_'||ch=='-'?ch:'_').ToArray(); var res=new string(ok).Trim('_'); if(res.Length==0) res="t"; return res.Length<=64?res:res[..64]; }
}
