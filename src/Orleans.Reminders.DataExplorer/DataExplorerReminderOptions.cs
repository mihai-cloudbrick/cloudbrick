#nullable enable

namespace Cloudbrick.Orleans.Reminders.DataExplorer;
public sealed class DataExplorerReminderOptions
{
    public string DatabaseId { get; set; } = "orleans-reminders";
    public string TablePrefix { get; set; } = "Reminders";
    public int Buckets { get; set; } = 256;
    public bool CreateStructuresIfMissing { get; set; } = true;
    public Func<string, string, string> BuildItemId { get; set; } = (grainId, name) => StorageIdHasher.Sha256Hex($"{grainId}|{name}");
}
