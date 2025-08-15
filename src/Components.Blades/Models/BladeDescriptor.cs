using Microsoft.AspNetCore.Components;

namespace Cloudbrick.Components.Blades.Models;

public sealed class BladeDescriptor
{
    public required string Key { get; init; }
    public required Type Component { get; init; }
    public Dictionary<string, object?> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);
    public BladeSize Size { get; set; } = BladeSize.Default;
    public bool Pinned { get; set; } = false;

    public RenderFragment Render() => builder =>
    {
        builder.OpenComponent(0, Component);
        int seq = 1;
        foreach (var kvp in Parameters)
        {
            builder.AddAttribute(seq++, kvp.Key, kvp.Value);
        }
        builder.CloseComponent();
    };
}
