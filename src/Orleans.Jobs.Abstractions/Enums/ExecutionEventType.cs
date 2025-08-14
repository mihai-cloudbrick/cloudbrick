namespace Cloudbrick.Orleans.Jobs.Abstractions.Enums;

public enum ExecutionEventType
{
    StatusChanged,
    Progress,
    Log,
    Error,
    Completed,
    Flushed,
    Custom,
    // NEW: job-level snapshot (aggregate counters + %)
    JobSnapshot,
}
