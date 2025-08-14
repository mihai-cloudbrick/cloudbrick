using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.GrainIds;


/// <summary>
/// A lightweight wrapper for a JSON (or Base64-JSON) grain key.
/// Use Create(...) to build, or implicit conversions to/from string.
/// </summary>
public readonly struct CompoundJsonKey<T>
{
    public string Key { get; }
    public JsonKeyFormat Format { get; }

    private CompoundJsonKey(string key, JsonKeyFormat format)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Format = format;
    }

    public static CompoundJsonKey<T> Create(T value, JsonKeyFormat format = JsonKeyFormat.PlainJson)
        => new(GrainKeyJsonHelper.ToKey(value, format), format);

    public static T Parse(string key, JsonKeyFormat format = JsonKeyFormat.Auto)
        => GrainKeyJsonHelper.FromKey<T>(key, format);

    public override string ToString() => Key;

    public static implicit operator string(CompoundJsonKey<T> key) => key.Key;
    public static implicit operator CompoundJsonKey<T>(string key) => new(key, JsonKeyFormat.Auto);
}
