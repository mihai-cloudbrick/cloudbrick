namespace Cloudbrick.Orleans.Jobs.Abstractions.Enums;

public enum JobTaskStatus
{
    Created = 0,
    Queued = 1,
    Running = 2,
    Paused = 3,
    Cancelling = 4,
    Cancelled = 5,
    Succeeded = 6,
    Failed = 7,
    Scheduled = 8
}
