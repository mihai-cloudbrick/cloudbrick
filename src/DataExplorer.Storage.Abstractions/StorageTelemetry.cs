#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Diagnostics;
namespace Cloudbrick.DataExplorer.Storage.Abstractions;
public static class StorageTelemetry
{
    public const string ActivitySourceName = "Cloudbrick.DataExplorer.Storage";
    public static readonly ActivitySource Source = new(ActivitySourceName);
}
