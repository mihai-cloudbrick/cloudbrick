using System.Collections.Generic;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models;

public class TaskSpec
{
    public string TaskId { get; set; } = string.Empty;
    public string ExecutorType { get; set; } = string.Empty;
    public string CommandJson { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = new List<string>();
    public int MaxRetries { get; set; } = 0;
    public int RetryBackoffSeconds { get; set; } = 2;
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    public string? CorrelationId { get; set; }
    // NEW — CRON scheduling (optional)
    public string? Cron { get; set; }                // e.g., "*/5 * * * *" or "*/10 * * * * *" (with seconds)
    public string? CronTimeZone { get; set; }        // e.g., "UTC" (default), "Europe/Bucharest"
    public bool AllowConcurrentRuns { get; set; }    // default: false (skip if previous still running)
    public int? MaxRuns { get; set; }                // stop after N fires
    public DateTimeOffset? NotBefore { get; set; }   // don’t run before this time
    public DateTimeOffset? NotAfter { get; set; }    // don’t run after this time
}
