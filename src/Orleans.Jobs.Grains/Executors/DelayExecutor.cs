using System;
using System.Threading;
using System.Threading.Tasks;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

namespace Cloudbrick.Orleans.Jobs.Executors;

public class DelayExecutor : TaskExecutorBase<DelayExecutor.DelayCommand>
{
    public override string ExecutorType => "delay";

    public class DelayCommand
    {
        public int Milliseconds { get; set; } = 100;
        public int Steps { get; set; } = 5;
    }

    protected override async Task OnExecuteAsync(DelayCommand cmd, ITaskExecutionContext ctx, CancellationToken ct)
    {
        var perStep = Math.Max(1, cmd.Steps);
        for (int i = 1; i <= perStep; i++)
        {
            await ctx.WaitIfPausedAsync(ct);
            ct.ThrowIfCancellationRequested();
            await Task.Delay(cmd.Milliseconds, ct);
            await ctx.ReportProgressAsync((int)(i * 100.0 / perStep), $"Step {i}/{perStep}");
        }
    }
}
