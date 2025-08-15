#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class LogEntry
{
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Level { get; set; } = "Info"; // Info, Warn, Error, Debug, Trace
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
    public IReadOnlyDictionary<string, string>? Properties { get; set; }
}
