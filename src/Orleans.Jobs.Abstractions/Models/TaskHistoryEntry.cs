using System;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models;

public class TaskHistoryEntry
{
    public DateTimeOffset Timestamp { get; set; }
    public string Message { get; set; } = string.Empty;
}
