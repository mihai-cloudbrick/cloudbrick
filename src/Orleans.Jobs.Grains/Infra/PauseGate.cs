using System.Threading;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.Jobs.Infra;

public class PauseGate
{
    // Start in RESUMED state (completed TCS)
    private volatile TaskCompletionSource<bool> _tcs = CompletedTcs();

    private static TaskCompletionSource<bool> CompletedTcs()
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        tcs.TrySetResult(true); // completed => resumed
        return tcs;
    }

    private static TaskCompletionSource<bool> NewPausedTcs() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously); // not completed => paused

    public Task WaitAsync(CancellationToken ct)
    {
        var t = _tcs.Task;
        return t.IsCompleted ? Task.CompletedTask : t.WaitAsync(ct);
    }

    public void Pause()
    {
        var current = _tcs;
        if (!current.Task.IsCompleted) return; // already paused
        Interlocked.CompareExchange(ref _tcs, NewPausedTcs(), current);
    }

    public void Resume()
    {
        var current = _tcs;
        if (current.Task.IsCompleted) return; // already resumed
        current.TrySetResult(true); // resume all awaiters
    }

    public bool IsPaused => !_tcs.Task.IsCompleted;
}