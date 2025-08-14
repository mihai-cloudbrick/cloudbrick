#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Storage;

namespace Cloudbrick.Orleans.Reminders.DataExplorer;

public sealed class DataExplorerReminderTable : IReminderTable
{
    private readonly IConfigAwareStorageManager _manager;
    private readonly IOptions<DataExplorerReminderOptions> _options;
    private readonly ILogger<DataExplorerReminderTable> _log;
    private string _serviceId = "";
    private SiloAddress? _silo;
    public DataExplorerReminderTable(IConfigAwareStorageManager manager, IOptions<DataExplorerReminderOptions> options, ILogger<DataExplorerReminderTable> log) 
    { 
        _manager = manager; 
        _options = options; 
        _log = log; 
        Buckets.EnsurePowerOfTwo(_options.Value.Buckets); 
    }
    public async Task Init(string serviceId, SiloAddress address) 
    { 
        _serviceId = serviceId; 
        _silo = address; 
        if (_options.Value.CreateStructuresIfMissing) 
        { 
            var db = _manager.Database(_options.Value.DatabaseId); 
            await db.CreateIfNotExistsAsync(); 
        } 
    }
    public async Task<ReminderTableData> ReadRows(uint beginHash, uint endHash)
    {
        var opt = _options.Value; var db = _manager.Database(opt.DatabaseId); var rows = new List<ReminderEntry>();
        for (int b = 0; b < opt.Buckets; b++)
        {
            if (!Buckets.RangeIntersectsBucket(beginHash, endHash, b, opt.Buckets)) continue; 
            var table = db.Table(Buckets.TableFor(opt.TablePrefix, b));
            var skip = 0; const int page = 256;
            while (true) { var batch = await table.ListAsync(skip, page); 
                if (batch.Items is null || batch.Items.Count == 0) 
                    break; 
                foreach (var it in batch.Items) 
                { 
                    var doc = ReminderDocument.FromJson(it.Data); 
                    if (!string.Equals(doc.ServiceId, _serviceId, StringComparison.Ordinal)) 
                        continue; 
                    if (doc.Bucket != b) 
                        continue; 
                    if (!Buckets.InRange(doc.GrainHash, beginHash, endHash)) 
                        continue; 
                    rows.Add(ToEntry(doc, it.ETag)); 
                } 
                if (batch.Items.Count < page) 
                    break; 
                skip += page;
            }
        }
        return new ReminderTableData(rows);
    }
    public async Task<ReminderTableData> ReadRows(GrainId grainId, string reminderName) 
    { 
        var e = await ReadRow(grainId, reminderName); 
        return new ReminderTableData(e is null ? new List<ReminderEntry>() : new List<ReminderEntry> { e }); 
    }
    public async Task<string> UpsertRow(ReminderEntry entry)
    {
        var opt = _options.Value; 
        var db = _manager.Database(opt.DatabaseId); 
        var gid = entry.GrainId.ToString(); 
        var hash = unchecked(entry.GrainId.GetUniformHashCode()); 
        var bucket = Buckets.Index(hash, opt.Buckets); 
        var table = db.Table(Buckets.TableFor(opt.TablePrefix, bucket));
        var doc = new ReminderDocument { ServiceId = _serviceId, GrainId = gid, ReminderName = entry.ReminderName, StartAtUtc = entry.StartAt, Period = entry.Period, GrainHash = hash, Bucket = bucket };
        var id = opt.BuildItemId(gid, entry.ReminderName); 
        var item = new StorageItem { Data = doc.ToJson(), ETag = entry.ETag };
        StorageResult<StorageItem> res;
        if (string.IsNullOrEmpty(entry.ETag)) 
        { 
            res = await table.CreateAsync(id, item); 
            if (res.Status == OperationStatus.Conflict) 
                res = await table.UpdateAsync(id, item); 
        }
        else 
        { 
            res = await table.UpdateAsync(id, item); 
            if (res.Status == OperationStatus.Conflict) throw new InconsistentStateException($"ETag conflict for reminder {entry.GrainId}/{entry.ReminderName}"); 
        }
        return res.ETag ?? entry.ETag ?? string.Empty;
    }
    public async Task<bool> RemoveRow(GrainId grainId, string reminderName, string eTag)
    {
        var opt = _options.Value; 
        var db = _manager.Database(opt.DatabaseId); 
        var gid = grainId.ToString(); 
        var hash = unchecked(grainId.GetUniformHashCode()); 
        var bucket = Buckets.Index(hash, opt.Buckets); 
        var table = db.Table(Buckets.TableFor(opt.TablePrefix, bucket)); 
        var id = opt.BuildItemId(gid, reminderName);
        var del = await table.DeleteAsync(id);
        if (del.Status == OperationStatus.Conflict) throw new InconsistentStateException($"ETag conflict removing reminder {grainId}/{reminderName}"); 
        return del.Status == OperationStatus.Deleted || del.Status == OperationStatus.NotFound;
    }
    public async Task TestOnlyClearTable()
    {
        var opt = _options.Value; var db = _manager.Database(opt.DatabaseId);
        for (int b = 0; b < opt.Buckets; b++)
        {
            var table = db.Table(Buckets.TableFor(opt.TablePrefix, b)); 
            var skip = 0; const int page = 256;
            while (true) 
            {
                var batch = await table.ListAsync(skip, page); 
                if (batch.Items is null || batch.Items.Count == 0) 
                    break; 
                foreach (var it in batch.Items) 
                { 
                    var doc = ReminderDocument.FromJson(it.Data); 
                    if (!string.Equals(doc.ServiceId, _serviceId, StringComparison.Ordinal)) 
                        continue; 
                    await table.DeleteAsync(opt.BuildItemId(doc.GrainId, doc.ReminderName)); 
                } 
                if (batch.Items.Count < page) 
                    break; 
                skip += page; }
        }
    }
    private async Task<ReminderEntry?> ReadRow(GrainId grainId, string reminderName)
    {
        var opt = _options.Value; 
        var db = _manager.Database(opt.DatabaseId); 
        var gid = grainId.ToString(); 
        var hash = unchecked(grainId.GetUniformHashCode()); 
        var bucket = Buckets.Index(hash, opt.Buckets); 
        var table = db.Table(Buckets.TableFor(opt.TablePrefix, bucket)); 
        var id = opt.BuildItemId(gid, reminderName);
        var res = await table.GetAsync(id); 
        if (res.Status == OperationStatus.NotFound || res.Item is null) 
            return null; 
        var doc = ReminderDocument.FromJson(res.Item.Data); 
        if (!string.Equals(doc.ServiceId, _serviceId, StringComparison.Ordinal)) 
            return null; 
        return ToEntry(doc, res.ETag);
    }
    private static ReminderEntry ToEntry(ReminderDocument doc, string? etag) => new ReminderEntry
    {
        GrainId = GrainId.Parse(doc.GrainId),
        ReminderName = doc.ReminderName,
        StartAt = doc.StartAtUtc,
        ETag = etag ?? string.Empty,
        Period = doc.Period,
    };

    public Task<ReminderTableData> ReadRows(GrainId grainId)
    {
        throw new NotImplementedException();
    }

    Task<ReminderEntry> IReminderTable.ReadRow(GrainId grainId, string reminderName)
    {
        return ReadRow(grainId, reminderName);
    }
}
