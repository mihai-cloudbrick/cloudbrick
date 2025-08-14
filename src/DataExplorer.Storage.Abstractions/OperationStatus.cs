namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public enum OperationStatus
{
    None,
    Created,
    Updated,
    Deleted,
    NotFound,
    Conflict,
    Unchanged,
    Error
}
