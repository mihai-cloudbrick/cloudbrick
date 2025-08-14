#nullable enable
using Cloudbrick.DataExplorer.Storage.Provider.FileSystem;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cloudbrick.DataExplorer.Storage.Provider.FileSystem;

internal static class JsonSerializerFactory
{
    public static JsonSerializerOptions Create()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };
    }
}
