using System;
using System.Collections.Generic;
using System.Linq;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

namespace Cloudbrick.Orleans.Jobs.Executors;

public class ExecutorFactory : ITaskExecutorFactory
{
    private readonly Dictionary<string, ITaskExecutor> _map;

    public ExecutorFactory(IEnumerable<ITaskExecutor> executors)
    {
        _map = executors.ToDictionary(e => e.ExecutorType, StringComparer.OrdinalIgnoreCase);
    }

    public ITaskExecutor Resolve(string executorType)
    {
        if (!_map.TryGetValue(executorType, out var exec))
            throw new InvalidOperationException($"No executor registered for type '{executorType}'.");
        return exec;
    }
}
