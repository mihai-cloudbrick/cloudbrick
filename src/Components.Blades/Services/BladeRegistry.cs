using Cloudbrick.Components.Blades.Models;
using Microsoft.AspNetCore.Components;

namespace Cloudbrick.Components.Blades.Services;

internal sealed class BladeRegistry : IBladeRegistry
{
    private readonly Dictionary<string, Type> _map = new(StringComparer.OrdinalIgnoreCase);

    public IBladeRegistry Add<TComponent>(string key) where TComponent : IComponent
    {
        _map[key] = typeof(TComponent);
        return this;
    }

    public bool TryResolve(string key, out Type? componentType)
    {
        var ok = _map.TryGetValue(key, out var t);
        componentType = t;
        return ok;
    }

    public IEnumerable<string> Keys => _map.Keys;
}
