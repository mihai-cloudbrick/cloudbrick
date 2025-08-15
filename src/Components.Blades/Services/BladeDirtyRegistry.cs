using Cloudbrick.Components.Blades.Models;

namespace Cloudbrick.Components.Blades.Services;

internal sealed class BladeDirtyRegistry : IBladeDirtyRegistry
{
    private readonly Dictionary<BladeDescriptor, (bool Dirty, Func<Task<bool>>? Confirm)> _map = new();

    public void SetDirty(BladeDescriptor blade, bool isDirty, Func<Task<bool>>? confirmAsync = null)
        => _map[blade] = (isDirty, confirmAsync);

    public bool IsDirty(BladeDescriptor blade)
        => _map.TryGetValue(blade, out var v) && v.Dirty;

    public Task<bool> CanCloseAsync(BladeDescriptor blade)
    {
        if (!_map.TryGetValue(blade, out var v) || !v.Dirty)
            return Task.FromResult(true);

        if (v.Confirm is null) return Task.FromResult(false);
        return v.Confirm.Invoke();
    }
}
