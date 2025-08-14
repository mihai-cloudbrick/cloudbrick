#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;
using Cloudbrick.DataExplorer.Storage.Configuration;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

/// <summary>
/// Provider-agnostic JSON diff helper. Produces a dictionary keyed by JSON path
/// (e.g., "Data.Address.City") with <see cref="ChangeRecord"/> describing the change.
/// Designed for auditing: includes the PrincipalId and a timestamp on each record.
///
/// Supports array normalization: items can be ordered by configured property keys
/// (e.g., "Id", "Name", "Timestamp") before diffing, so reordering alone doesn't
/// produce spurious changes.
/// </summary>
public class JsonDiff
{
    private readonly JsonDiffOptions? _options;
    /// <summary>Global defaults used when no options instance is provided.</summary>
    public JsonDiff(JsonDiffOptions? options)
    {
        _options = options ?? new JsonDiffOptions();
    }
    private static JsonNode? DefaultRedactor(JsonNode? _)
    => JsonValue.Create("REDACTED");
    /// <summary>
    /// Computes a JSON diff between <paramref name="oldObj"/> and <paramref name="newObj"/>.
    /// Arrays can be normalized (sorted) by configured property keys before diffing.
    /// </summary>
    public IReadOnlyDictionary<string, ChangeRecord> Compute(
        JsonObject? oldObj,
        JsonObject? newObj,
        string principalId)
    {
        var opt = _options;
        var changes = new Dictionary<string, ChangeRecord>();
        Recurse(opt.RootPath, oldObj ?? new JsonObject(), newObj ?? new JsonObject(), depth: 0);
        return changes;

        void Recurse(string path, JsonNode? oldNode, JsonNode? newNode, int depth)
        {
            if (changes.Count >= opt.MaxChanges || depth > opt.MaxDepth) return;
            if (IsFiltered(path, opt)) return;

            // Equal?
            if (JsonEqual(oldNode, newNode)) return;

            // If either side missing or type differs -> record leaf
            if (oldNode is null || newNode is null || oldNode.GetType() != newNode.GetType())
            {
                Add(path, oldNode, newNode);
                return;
            }

            // Objects
            if (oldNode is JsonObject oo && newNode is JsonObject no)
            {
                var keys = new HashSet<string>(oo.Select(kv => kv.Key).Concat(no.Select(kv => kv.Key)));
                foreach (var key in keys)
                {
                    if (changes.Count >= opt.MaxChanges) break;
                    Recurse(string.IsNullOrWhiteSpace(path) ? key : $"{path}.{key}", oo[key], no[key], depth + 1);
                }
                return;
            }

            // Arrays (normalize → compare → (optionally) element-by-element)
            if (oldNode is JsonArray oa && newNode is JsonArray na)
            {
                var (oNorm, nNorm) = NormalizeArraysIfNeeded(oa, na, opt);

                if (JsonEqual(oNorm, nNorm))
                {
                    // Only order changed
                    return;
                }

                if (!opt.DiffArrays)
                {
                    Add(path, oa, na); // record as leaf difference with ORIGINAL arrays
                    return;
                }

                var max = Math.Min(Math.Max(oNorm.Count, nNorm.Count), opt.MaxArrayItems);
                for (var i = 0; i < max; i++)
                {
                    if (changes.Count >= opt.MaxChanges) break;

                    var o = i < oNorm.Count ? oNorm[i] : null;
                    var n = i < nNorm.Count ? nNorm[i] : null;
                    Recurse($"{path}[{i}]", o, n, depth + 1);
                }

                // If lengths exceed MaxArrayItems, record a summary change at the path
                if (Math.Max(oNorm.Count, nNorm.Count) > opt.MaxArrayItems)
                    Add(path, oa, na);

                return;
            }

            // Primitives
            Add(path, oldNode, newNode);
        }

        void Add(string p, JsonNode? o, JsonNode? n)
        {
            if (changes.Count >= opt.MaxChanges) return;
            if (IsFiltered(p, opt)) return;

            // Keep ORIGINAL values (pre-normalization) for audit, then redact if needed.
            JsonNode? oldOut = o?.DeepClone();
            JsonNode? newOut = n?.DeepClone();

            if (opt.RedactPath?.Invoke(p) == true)
            {
                var redactor = opt.RedactValue ?? DefaultRedactor;
                oldOut = redactor(oldOut);
                newOut = redactor(newOut);
            }

            changes[p] = new ChangeRecord
            {
                OldValue = oldOut,
                NewValue = newOut,
                PrincipalId = principalId,
                TimestampUtc = DateTimeOffset.UtcNow
            };
        }
    }

    // ---------- Array normalization (ordering) ----------

    private (JsonArray O, JsonArray N) NormalizeArraysIfNeeded(JsonArray oa, JsonArray na, JsonDiffOptions opt)
    {
        if (!opt.NormalizeArraysBeforeDiff || opt.ArrayOrderKeys.Count == 0)
            return (oa, na);

        var o = NormalizeArray(oa, opt);
        var n = NormalizeArray(na, opt);
        return (o, n);
    }

    private JsonArray NormalizeArray(JsonArray source, JsonDiffOptions opt)
    {
        // Clone each element so we don't attach a node that already has a parent.
        var items = new List<JsonNode?>(source.Select(n => n?.DeepClone()));

        // Sort the CLONED items using the configured comparer.
        items.Sort(new ArrayItemComparer(opt));

        // Build a fresh JsonArray from the clones.
        var clone = new JsonArray();
        foreach (var node in items)
            clone.Add(node); // safe: nodes here have no parent

        return clone;
    }

    private sealed class ArrayItemComparer : IComparer<JsonNode?>
    {
        private readonly JsonDiffOptions _opt;
        private readonly StringComparer _nameKeyComparer;
        private readonly StringComparison _stringComparison;

        public ArrayItemComparer(JsonDiffOptions opt)
        {
            _opt = opt;
            _nameKeyComparer = opt.CaseInsensitivePropertyLookup ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            _stringComparison = opt.CaseInsensitivePropertyLookup ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }

        public int Compare(JsonNode? x, JsonNode? y)
        {
            // Nulls last
            if (x is null && y is null) return 0;
            if (x is null) return 1;
            if (y is null) return -1;

            // Objects: compare by the first key that exists on at least one side
            if (x is JsonObject xo && y is JsonObject yo && _opt.ArrayOrderKeys.Count > 0)
            {
                foreach (var key in _opt.ArrayOrderKeys)
                {
                    var (hasX, vx) = TryGetProperty(xo, key);
                    var (hasY, vy) = TryGetProperty(yo, key);

                    if (!hasX && !hasY) continue;
                    if (hasX && !hasY) return -1;  // items with the key come first
                    if (!hasX && hasY) return 1;

                    var cmp = CompareValues(vx, vy);
                    if (cmp != 0) return cmp;
                }
            }

            // Fallback: compare canonical JSON strings
            return string.Compare(AsCanonical(x), AsCanonical(y), StringComparison.Ordinal);
        }

        private (bool Has, JsonNode? Value) TryGetProperty(JsonObject obj, string name)
        {
            if (!_opt.CaseInsensitivePropertyLookup)
                return obj.TryGetPropertyValue(name, out var v) ? (true, v) : (false, null);

            // Case-insensitive lookup
            foreach (var kv in obj)
                if (_nameKeyComparer.Equals(kv.Key, name))
                    return (true, kv.Value);
            return (false, null);
        }

        private int CompareValues(JsonNode? a, JsonNode? b)
        {
            // Nulls last
            if (a is null && b is null) return 0;
            if (a is null) return 1;
            if (b is null) return -1;

            // Numbers
            if (a is JsonValue va && va.TryGetValue(out double da) &&
                b is JsonValue vb && vb.TryGetValue(out double db))
            {
                return da.CompareTo(db);
            }

            // Booleans
            if (a is JsonValue va2 && va2.TryGetValue(out bool ba) &&
                b is JsonValue vb2 && vb2.TryGetValue(out bool bb))
            {
                return ba.CompareTo(bb);
            }

            // Datetimes or strings
            if (AsString(a, out var sa) && AsString(b, out var sb))
            {
                if (_opt.TreatStringsAsDateTimeWhenPossible &&
                    DateTimeOffset.TryParse(sa, out var da3) &&
                    DateTimeOffset.TryParse(sb, out var db3))
                {
                    return da3.CompareTo(db3);
                }
                return string.Compare(sa, sb, _stringComparison);
            }

            // Objects/Arrays: fallback to canonical
            return string.Compare(AsCanonical(a), AsCanonical(b), StringComparison.Ordinal);
        }

        private static bool AsString(JsonNode node, out string value)
        {
            if (node is JsonValue v && v.TryGetValue(out string? s))
            {
                value = s;
                return true;
            }
            value = node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            return false;
        }

        private static string AsCanonical(JsonNode node)
            => node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    // ---------- helpers ----------

    private  bool JsonEqual(JsonNode? a, JsonNode? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (ReferenceEquals(a, b)) return true;

        return a.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ==
               b.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private  bool IsFiltered(string path, JsonDiffOptions opt)
    {
        if (opt.AllowPath is not null && !opt.AllowPath(path)) return true;
        if (opt.DenyPath is not null && opt.DenyPath(path)) return true;
        return false;
    }
}
