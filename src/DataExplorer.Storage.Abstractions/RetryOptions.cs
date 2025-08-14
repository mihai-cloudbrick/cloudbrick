#nullable enable
namespace Cloudbrick.DataExplorer.Storage.Abstractions;
public sealed record RetryOptions
{
    public bool Enabled { get; init; } = true;
    public int MaxAttempts { get; init; } = 3;
    public int BaseDelayMs { get; init; } = 200;
    public int MaxJitterMs { get; init; } = 200;
    public bool HandleTimeouts { get; init; } = true;
}
