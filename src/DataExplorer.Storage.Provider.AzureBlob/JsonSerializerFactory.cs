#nullable enable
using Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cloudbrick.DataExplorer.Storage.Provider.AzureBlob;

internal static class JsonSerializerFactory
{
    public static JsonSerializerOptions Create() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}
