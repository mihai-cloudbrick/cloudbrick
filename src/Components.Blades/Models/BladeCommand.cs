using Microsoft.AspNetCore.Components;

namespace Cloudbrick.Components.Blades.Models;

public sealed class BladeCommand
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public string? IconName { get; init; }
    public Func<Task<bool>>? CanExecuteAsync { get; init; }
    public required Func<Task> ExecuteAsync { get; init; }
    public bool IsPrimary { get; init; }
}
