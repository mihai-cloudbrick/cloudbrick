namespace Cloudbrick.Orleans.Jobs.Abstractions;

public static class StreamConstants
{
    public const string ProviderName = "JobEngineSMS";
    public const string TaskNamespace = "task.telemetry";
    public const string JobNamespace = "job.telemetry";
    public const string TaskControlNamespace = "task.control";  // <—
}
