#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using System.Runtime.ExceptionServices;
namespace Cloudbrick.DataExplorer.Storage.Abstractions;
public static class RetryPolicyFactory
{
    public static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, RetryOptions? opt, CancellationToken ct)
    {
        opt ??= new RetryOptions();
        var attempts = Math.Max(1, opt.MaxAttempts);
        var rnd = new Random();
        Exception? last = null;
        for (int tryNo = 1; tryNo <= attempts; tryNo++)
        {
            ct.ThrowIfCancellationRequested();
            try { return await action(ct).ConfigureAwait(false); }
            catch (Exception ex) when (IsTransient(ex, opt))
            {
                last = ex;
                if (tryNo == attempts) break;
                var backoff = opt.BaseDelayMs * Math.Pow(2, tryNo - 1);
                var jitter = rnd.Next(0, Math.Max(1, opt.MaxJitterMs));
                await Task.Delay(TimeSpan.FromMilliseconds(backoff + jitter), ct).ConfigureAwait(false);
            }
        }
        ExceptionDispatchInfo.Capture(last!).Throw();
        throw last!;
    }
    private static bool IsTransient(Exception ex, RetryOptions opt)
    {
        var msg = ex.Message?.ToLowerInvariant() ?? "";
        if (opt.HandleTimeouts && (ex is TimeoutException || msg.Contains("timeout"))) return true;
        return false;
    }
}
