using Cloudbrick.Components.Blades.Models;

namespace Cloudbrick.Components.Blades.Services;

public interface IBladeManager
{
    event EventHandler? Changed;

    IReadOnlyList<BladeDescriptor> Stack { get; }

    Task OpenAsync(string key, IDictionary<string, object?>? parameters = null, BladeSize size = BladeSize.Default, bool pinned = false);
    Task CloseRightOfAsync(int indexInclusive);
    Task CloseAsync(int index);
    Task ReplaceRightOfAsync(int indexInclusive, string key, IDictionary<string, object?>? parameters = null, BladeSize size = BladeSize.Default);

    // URL sync helpers
    void RestoreFromRoute(IEnumerable<BladeRouteItem> items);
    IEnumerable<BladeRouteItem> ToRouteItems();
}
