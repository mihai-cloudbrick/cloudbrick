namespace Cloudbrick.Components.Blades.Models;

public sealed record BladeRouteItem(string Key, IReadOnlyDictionary<string, string> Parameters);
