using Cloudbrick.Components.Blades.Models;
using Microsoft.AspNetCore.Components;

namespace Cloudbrick.Components.Blades.Services;

public interface IBladeRegistry
{
    IBladeRegistry Add<TComponent>(string key) where TComponent : IComponent;
    bool TryResolve(string key, out Type? componentType);
    IEnumerable<string> Keys { get; }
}
