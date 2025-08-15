using System.Diagnostics.CodeAnalysis;

namespace Cloudbrick.Components.Blades.Routing;

public sealed class ArmRoute
{
    public required string SubscriptionId { get; init; }
    public string? ResourceGroupName { get; init; }
    public string? ProviderNamespace { get; init; }
    public List<(string Type, string? Name)> Segments { get; } = new();
}

internal static class ArmPathParser
{
    public static bool TryParse(string baseRelativePath, [NotNullWhen(true)] out ArmRoute? route)
    {
        route = null;
        if (string.IsNullOrWhiteSpace(baseRelativePath)) return false;

        var parts = baseRelativePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        if (!parts[0].Equals("subscriptions", StringComparison.OrdinalIgnoreCase) || parts.Length < 2)
            return false;

        var subId = Uri.UnescapeDataString(parts[1]);
        string? rgName = null;
        string? ns = null;

        int i = 2;
        if (i + 1 < parts.Length && parts[i].Equals("resourceGroups", StringComparison.OrdinalIgnoreCase))
        {
            rgName = Uri.UnescapeDataString(parts[i + 1]);
            i += 2;
        }

        var segs = new List<(string Type, string? Name)>();

        if (i < parts.Length)
        {
            if (!parts[i].Equals("providers", StringComparison.OrdinalIgnoreCase) || i + 1 >= parts.Length)
                return false;

            ns = Uri.UnescapeDataString(parts[i + 1]);
            i += 2;

            while (i < parts.Length)
            {
                var type = Uri.UnescapeDataString(parts[i]);
                string? name = null;

                if (i + 1 < parts.Length)
                {
                    name = Uri.UnescapeDataString(parts[i + 1]);
                    i += 2;
                }
                else
                {
                    i += 1;
                }

                segs.Add((type, name));
            }
        }

        route = new ArmRoute
        {
            SubscriptionId = subId,
            ResourceGroupName = rgName,
            ProviderNamespace = ns,
        };
        route.Segments.AddRange(segs);
        return true;
    }
}
