#nullable enable
using System.Text.Json;
using System.Text.Json.Nodes;
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Orleans.Storage;

namespace Cloudbrick.Orleans.Persistance.DataExplorer;

public sealed class DataExplorerGrainStorage : IGrainStorage
{
    private readonly IConfigAwareStorageManager _manager;
    private readonly IOptionsMonitor<DataExplorerOrleansStorageOptions> _options;
    private readonly string _name;
    private readonly ILogger<DataExplorerGrainStorage> _log;
    public DataExplorerGrainStorage(string name, IConfigAwareStorageManager manager, IOptionsMonitor<DataExplorerOrleansStorageOptions> options, ILogger<DataExplorerGrainStorage> log){ _name=name; _manager=manager; _options=options; _log=log; }
    public async Task ClearStateAsync<T>(string grainType, GrainId grainRef, IGrainState<T> grainState)
    {
        var (db, table, id) = Map(grainType, grainRef);
        var res = await _manager.Database(db).Table(table).DeleteAsync(id);
        if (res.Status == OperationStatus.Conflict) throw new InconsistentStateException($"ETag conflict clearing {grainType}/{id}");
        grainState.RecordExists = false; grainState.ETag = null; grainState.State = default!;
    }
    public async Task ReadStateAsync<T>(string grainType, GrainId grainRef, IGrainState<T> grainState)
    {
        var opts = _options.Get(_name);
        if (opts.CreateStructuresIfMissing) { /* no-op for FS */ }
        var (db, table, id) = Map(grainType, grainRef);
        var res = await _manager.Database(db).Table(table).GetAsync(id);
        if (res.Status == OperationStatus.NotFound){ grainState.RecordExists=false; grainState.ETag=null; grainState.State=default!; return; }
        grainState.State = Deserialize<T>(res.Item!.Data, opts.JsonOptions); grainState.ETag = res.ETag; grainState.RecordExists = true;
    }
    public async Task WriteStateAsync<T>(string grainType, GrainId grainRef, IGrainState<T> grainState)
    {
        var (db, table, id) = Map(grainType, grainRef);
        var opts = _options.Get(_name);
        var json = Serialize(grainState.State, opts.JsonOptions);
        var item = new StorageItem { Data = json, ETag = grainState.ETag };
        StorageResult<StorageItem> res;
        if (string.IsNullOrEmpty(grainState.ETag)){ res = await _manager.Database(db).Table(table).CreateAsync(id, item); if (res.Status == OperationStatus.Conflict) res = await _manager.Database(db).Table(table).UpdateAsync(id, item); }
        else { res = await _manager.Database(db).Table(table).UpdateAsync(id, item); if (res.Status == OperationStatus.Conflict) throw new InconsistentStateException($"ETag conflict writing {grainType}/{id}"); }
        grainState.ETag = res.ETag ?? grainState.ETag; grainState.RecordExists = true;
    }
    private (string DatabaseId, string TableId, string ItemId) Map(string grainType, GrainId grainRef)
    {
        var opts = _options.Get(_name); 
        var pk = grainRef.ToString(); 
        var (db, table, id) = opts.Mapper(grainType, pk, null);
        db = string.IsNullOrWhiteSpace(db) ? opts.DefaultDatabaseId : db; 
        table ??= opts.DefaultTableId ?? StorageNameSanitizer.Sanitize(grainType); 
        return (db, table, id);
    }
    public (string DatabaseId, string TableId, string ItemId) MapForTest(string grainType, string primary, string? ext)
    {
        var opts = _options.Get(_name); 
        var (db, table, id) = opts.Mapper(grainType, primary, ext);
        db = !string.IsNullOrWhiteSpace(db) ? opts.DefaultDatabaseId : db; 
        table ??= opts.DefaultTableId ?? StorageNameSanitizer.Sanitize(grainType); 
        return (db, table, id);
    }
    private static JsonObject Serialize<T>(T state, JsonSerializerOptions options) => JsonSerializer.SerializeToNode(state, options) as JsonObject ?? new JsonObject();
    private static T Deserialize<T>(JsonObject data, JsonSerializerOptions options) => JsonSerializer.Deserialize<T>(data.ToJsonString(), options)!;
}
