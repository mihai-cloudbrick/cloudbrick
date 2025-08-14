namespace Cloudbrick.Orleans.Jobs.Abstractions.Enums;

public enum JobStatus
{
    Created = 0,
    Running = 1,
    Paused = 2,
    Cancelling = 3,
    Cancelled = 4,
    Succeeded = 5,
    Failed = 6,
    PartiallySucceeded = 7
}
