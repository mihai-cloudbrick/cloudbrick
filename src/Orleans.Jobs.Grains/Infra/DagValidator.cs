using System.Collections.Generic;
using System.Linq;

namespace Cloudbrick.Orleans.Jobs.Infra;

public static class DagValidator
{
    public static bool HasCycle<TKey>(Dictionary<TKey, List<TKey>> graph)
        where TKey : notnull
    {
        var visited = new HashSet<TKey>();
        var inStack = new HashSet<TKey>();

        foreach (var node in graph.Keys)
        {
            if (Visit(node)) return true;
        }
        return false;

        bool Visit(TKey n)
        {
            if (inStack.Contains(n)) return true;
            if (visited.Contains(n)) return false;
            visited.Add(n);
            inStack.Add(n);
            if (graph.TryGetValue(n, out var deps))
            {
                foreach (var d in deps)
                {
                    if (Visit(d)) return true;
                }
            }
            inStack.Remove(n);
            return false;
        }
    }
}
