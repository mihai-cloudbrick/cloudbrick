using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Abstractions.GrainIds;


public static class GrainKeyJsonHelper
{
    // Public surface (Create/Parse)
    public static string ToKey<T>(T value, JsonKeyFormat format = JsonKeyFormat.PlainJson)
    {
        var json = CanonicalJson(value);
        return format switch
        {
            JsonKeyFormat.PlainJson => json,
            JsonKeyFormat.Base64Json => Convert.ToBase64String(Encoding.UTF8.GetBytes(json)),
            _ => json
        };
    }

    public static T FromKey<T>(string key, JsonKeyFormat format = JsonKeyFormat.Auto)
    {
        string json = format switch
        {
            JsonKeyFormat.PlainJson => key,
            JsonKeyFormat.Base64Json => Encoding.UTF8.GetString(Convert.FromBase64String(key)),
            JsonKeyFormat.Auto => LooksLikeJson(key) ? key : Encoding.UTF8.GetString(Convert.FromBase64String(key)),
            _ => key
        };
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    // ---- Canonical JSON (stable ordering) ----
    // Ensures the same object always serializes to the same string,
    // regardless of property declaration order.
    private static string CanonicalJson<T>(T value)
    {
        // First serialize with default options (for speed),
        // then re-emit with sorted property names recursively.
        var initial = JsonSerializer.Serialize(value, JsonOpts);
        using var doc = JsonDocument.Parse(initial);
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false });

        WriteCanonical(doc.RootElement, writer);
        writer.Flush();
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteCanonical(JsonElement element, Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var prop in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(prop.Name);
                    WriteCanonical(prop.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                    WriteCanonical(item, writer);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                // Preserve numeric text form
                writer.WriteRawValue(element.GetRawText());
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                writer.WriteRawValue(element.GetRawText());
                break;
        }
    }

    private static bool LooksLikeJson(string s)
    {
        // Quick heuristic: starts with '{' or '[' and ends with '}' or ']'
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        return s.Length >= 2 &&
               (s[0] == '{' && s[^1] == '}' || s[0] == '[' && s[^1] == ']');
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
