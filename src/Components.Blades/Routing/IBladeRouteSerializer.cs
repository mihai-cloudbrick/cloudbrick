using Cloudbrick.Components.Blades.Models;

namespace Cloudbrick.Components.Blades.Routing;

public interface IBladeRouteSerializer
{
    bool TryParse(string uri, out List<BladeRouteItem> items);
    string ToUri(string currentAbsoluteBase, IEnumerable<BladeRouteItem> items);
}
