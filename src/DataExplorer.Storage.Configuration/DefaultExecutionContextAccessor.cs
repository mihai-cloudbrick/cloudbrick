#nullable enable
using Cloudbrick.DataExplorer.Storage.Configuration;
using System.Threading;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

using Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// AsyncLocal-backed execution context accessor.
/// Use DI to set this singleton and set Current via helper methods in app code.
/// </summary>
public sealed class DefaultExecutionContextAccessor : IExecutionContextAccessor
{
    private static readonly AsyncLocal<IExecutionContext?> _current = new();

    public IExecutionContext? Current => _current.Value;

    // Helper for application code to set the current context inside a scope
    public IDisposable Push(IExecutionContext context)
    {
        var prior = _current.Value;
        _current.Value = context;
        return new Popper(prior);
    }

    private sealed class Popper : IDisposable
    {
        private readonly IExecutionContext? _prior;
        public Popper(IExecutionContext? prior) => _prior = prior;
        public void Dispose() => _current.Value = _prior;
    }
}
