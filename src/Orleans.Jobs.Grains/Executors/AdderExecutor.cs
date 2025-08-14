using System.Threading;
using System.Threading.Tasks;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

namespace Cloudbrick.Orleans.Jobs.Executors;

public class AdderExecutor : TaskExecutorBase<AdderExecutor.AddCommand>
{
    public override string ExecutorType => "adder";

    public class AddCommand
    {
        public int A { get; set; }
        public int B { get; set; }
    }

    protected override async Task OnExecuteAsync(AddCommand cmd, ITaskExecutionContext ctx, CancellationToken ct)
    {
        await ctx.ReportProgressAsync(50, "Adding...");
        var sum = cmd.A + cmd.B;
        await ctx.ReportProgressAsync(100, $"Result: {sum}");
    }
}
