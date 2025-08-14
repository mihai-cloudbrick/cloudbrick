using System.Collections.Generic;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Models;

public class JobSpec
{
    public string Name { get; set; } = "job";
    public Dictionary<string, TaskSpec> Tasks { get; set; } = new();
    public int MaxDegreeOfParallelism { get; set; } = 4;
    public bool FailFast { get; set; } = true;
    public string? CorrelationId { get; set; }
    public string? TelemetryProviderKey { get; set; }
}
