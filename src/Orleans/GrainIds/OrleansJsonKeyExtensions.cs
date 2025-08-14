using Orleans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.GrainIds;

public static class OrleansJsonKeyExtensions
{
    // Caller side: strongly typed creation
    public static TGrain GetGrainByJsonKey<TGrain, TKey>(
        this IGrainFactory grains,
        TKey keyObj,
        JsonKeyFormat format = JsonKeyFormat.PlainJson)
        where TGrain : IGrainWithStringKey
    {
        var key = CompoundJsonKey<TKey>.Create(keyObj, format);
        return grains.GetGrain<TGrain>(key);
    }

    // Caller side: with category (sharding / grouping if you use it)
    public static TGrain GetGrainByJsonKey<TGrain, TKey>(
        this IGrainFactory grains,
        TKey keyObj,
        string grainType,
        JsonKeyFormat format = JsonKeyFormat.PlainJson)
        where TGrain : IGrainWithStringKey
    {
        var key = CompoundJsonKey<TKey>.Create(keyObj, format);
        return grains.GetGrain<TGrain>(GrainId.Create(grainType, key));
    }
    public static TGrain GetGrainByBase64Key<TGrain, TKey>(
        this IGrainFactory grains,
        TKey keyObj,
        JsonKeyFormat format = JsonKeyFormat.Base64Json)
        where TGrain : IGrainWithStringKey
    {
        var key = CompoundJsonKey<TKey>.Create(keyObj, format);
        return grains.GetGrain<TGrain>(key);
    }

    // Caller side: with category (sharding / grouping if you use it)
    public static TGrain GetGrainByBase64Key<TGrain, TKey>(
        this IGrainFactory grains,
        TKey keyObj,
        string grainType,
        JsonKeyFormat format = JsonKeyFormat.Base64Json)
        where TGrain : IGrainWithStringKey
    {
        var key = CompoundJsonKey<TKey>.Create(keyObj, format);
        return grains.GetGrain<TGrain>(GrainId.Create(grainType, key));
    }
    // Inside Grain: parse the key back into the DTO
    public static TKey GetCompoundKey<TKey>(this Grain grain, JsonKeyFormat format = JsonKeyFormat.Auto)
    {
        var key = grain.GetPrimaryKeyString();
        return CompoundJsonKey<TKey>.Parse(key, format);
    }
}
