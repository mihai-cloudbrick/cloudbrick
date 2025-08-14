using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Infra;

public static class RetryPolicy
{
    public static async Task ExecuteAsync(Func<int, Task> action, int maxAttempts, int backoffSeconds, CancellationToken ct)
    {
        int attempt = 0;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await action(attempt + 1);
                return;
            }
            catch when (attempt < maxAttempts)
            {
                attempt++;
                var delay = TimeSpan.FromSeconds(Math.Pow(backoffSeconds, attempt));
                await Task.Delay(delay, ct);
            }
        }
    }
}
