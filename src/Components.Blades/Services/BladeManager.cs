using Cloudbrick.Components.Blades.Models;

namespace Cloudbrick.Components.Blades.Services;

internal sealed class BladeManager : IBladeManager
{
    private readonly IBladeDirtyRegistry _dirty;

    private readonly IBladeRegistry _registry;
    private readonly List<BladeDescriptor> _stack = new();

    public BladeManager(IBladeRegistry registry, IBladeDirtyRegistry dirty)
{
    _registry = registry;
    _dirty = dirty;
}

    public event EventHandler? Changed;
    public IReadOnlyList<BladeDescriptor> Stack => _stack;

    public Task OpenAsync(string key, IDictionary<string, object?>? parameters = null, BladeSize size = BladeSize.Default, bool pinned = false)
    {
        if (!_registry.TryResolve(key, out var type) || type is null)
            throw new InvalidOperationException($"Blade key '{key}' is not registered.");

        var desc = new BladeDescriptor
        {
            Key = key,
            Component = type,
            Size = size,
            Pinned = pinned
        };
        if (parameters != null)
        {
            foreach (var kv in parameters)
                desc.Parameters[kv.Key] = kv.Value;
        }

        _stack.Add(desc);
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public async Task CloseRightOfAsync(int indexInclusive)
    {
        if (_stack.Count == 0) return;
        indexInclusive = Math.Clamp(indexInclusive, 0, _stack.Count - 1);
        // check guards for all right-of items
        for (int i = _stack.Count - 1; i >= indexInclusive; i--)
        {
            var b = _stack[i];
            if (!await _dirty.CanCloseAsync(b)) return; // cancel entire close
        }
        _stack.RemoveRange(indexInclusive, _stack.Count - indexInclusive);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public async Task CloseAsync(int index)
    {
        if (index >= 0 && index < _stack.Count)
        {
            var b = _stack[index];
            if (!await _dirty.CanCloseAsync(b)) return;
            _stack.RemoveAt(index);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public Task ReplaceRightOfAsync(int indexInclusive, string key, IDictionary<string, object?>? parameters = null, BladeSize size = BladeSize.Default)
    {
        CloseRightOfAsync(indexInclusive).GetAwaiter().GetResult();
        return OpenAsync(key, parameters, size);
    }

    public void RestoreFromRoute(IEnumerable<BladeRouteItem> items)
    {
        _stack.Clear();
        foreach (var item in items)
        {
            if (!_registry.TryResolve(item.Key, out var t) || t is null)
                continue;

            var d = new BladeDescriptor { Key = item.Key, Component = t };
            foreach (var kv in item.Parameters)
                d.Parameters[kv.Key] = kv.Value;
            _stack.Add(d);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<BladeRouteItem> ToRouteItems()
        => _stack.Select(d =>
            new BladeRouteItem(d.Key,
                d.Parameters.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? "",
                    StringComparer.OrdinalIgnoreCase)));
}
