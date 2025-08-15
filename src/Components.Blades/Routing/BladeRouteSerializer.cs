using System.Text;
using System.Web;
using Cloudbrick.Components.Blades.Models;

namespace Cloudbrick.Components.Blades.Routing;

internal sealed class BladeRouteSerializer : IBladeRouteSerializer
{
    private const string ParamName = "b";

    public bool TryParse(string uri, out List<BladeRouteItem> items)
    {
        items = new();
        if (string.IsNullOrWhiteSpace(uri)) return false;

        var abs = new Uri(uri, UriKind.Absolute);
        var query = HttpUtility.ParseQueryString(abs.Query);
        var raw = query[ParamName];
        if (string.IsNullOrEmpty(raw)) return false;

        foreach (var seg in raw.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var s = seg.Trim();
            var open = s.IndexOf('(');
            var close = s.LastIndexOf(')');
            string key;
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (open > 0 && close > open)
            {
                key = s[..open];
                var inner = s.Substring(open + 1, close - open - 1);
                if (!string.IsNullOrWhiteSpace(inner))
                {
                    foreach (var pair in inner.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kv = pair.Split('=', 2);
                        var k = Uri.UnescapeDataString(kv[0].Trim());
                        var v = kv.Length > 1 ? Uri.UnescapeDataString(kv[1].Trim()) : "";
                        dict[k] = v;
                    }
                }
            }
            else
            {
                key = s;
            }

            if (!string.IsNullOrWhiteSpace(key))
                items.Add(new BladeRouteItem(key, dict));
        }

        return items.Count > 0;
    }

    public string ToUri(string currentAbsoluteBase, IEnumerable<BladeRouteItem> items)
    {
        var baseUri = new Uri(currentAbsoluteBase);
        var sb = new StringBuilder();
        bool first = true;

        foreach (var it in items)
        {
            if (!first) sb.Append('|'); else first = false;
            sb.Append(it.Key);
            if (it.Parameters.Count > 0)
            {
                sb.Append('(');
                bool pfirst = true;
                foreach (var kv in it.Parameters)
                {
                    if (!pfirst) sb.Append(','); else pfirst = false;
                    sb.Append(Uri.EscapeDataString(kv.Key));
                    sb.Append('=');
                    sb.Append(Uri.EscapeDataString(kv.Value ?? ""));
                }
                sb.Append(')');
            }
        }

        var bValue = sb.ToString();
        var ub = new UriBuilder(baseUri)
        {
            Query = $"b={Uri.EscapeDataString(bValue)}"
        };
        return ub.Uri.ToString();
    }
}
