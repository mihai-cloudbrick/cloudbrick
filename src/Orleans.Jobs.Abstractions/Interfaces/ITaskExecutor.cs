using System.Threading;
using System.Threading.Tasks;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

public interface ITaskExecutor
{
    string ExecutorType { get; }

    Task ValidateAsync(TaskSpec spec, ITaskExecutionContext ctx, CancellationToken ct);
    Task ExecuteAsync(ITaskExecutionContext ctx, CancellationToken ct);
    Task OnErrorAsync(Exception ex, ITaskExecutionContext ctx, CancellationToken ct);
    Task OnCompletedAsync(ITaskExecutionContext ctx, CancellationToken ct);
}
