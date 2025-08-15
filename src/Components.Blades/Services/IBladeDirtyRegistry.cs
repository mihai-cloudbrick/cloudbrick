using Cloudbrick.Components.Blades.Models;

namespace Cloudbrick.Components.Blades.Services;

public interface IBladeDirtyRegistry
{
    void SetDirty(BladeDescriptor blade, bool isDirty, Func<Task<bool>>? confirmAsync = null);
    bool IsDirty(BladeDescriptor blade);
    Task<bool> CanCloseAsync(BladeDescriptor blade);
}
