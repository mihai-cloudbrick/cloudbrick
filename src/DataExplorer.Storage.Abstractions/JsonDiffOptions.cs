using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cloudbrick.DataExplorer.Storage.Abstractions
{
    public sealed record JsonDiffOptions
    {
        /// <summary>Root path prefix used for keys (default: "Data").</summary>
        public string RootPath { get; set; } = "";

        /// <summary>Max number of changes to record (default: 512).</summary>
        public int MaxChanges { get; set; } = 512;

        /// <summary>Max recursion depth (default: 8).</summary>
        public int MaxDepth { get; set; } = 8;

        /// <summary>
        /// If true, arrays are diffed element-by-element with numeric indices (e.g., Data.Tags[0]).
        /// If false, any array difference is recorded as a single leaf change at that path.
        /// Default: false.
        /// </summary>
        public bool DiffArrays { get; set; } = false;

        /// <summary>
        /// Optional list of property names used to ORDER array items (case-insensitive)
        /// before comparison. If empty, arrays are not reordered.
        /// Example: ["Id", "Name", "Timestamp", "CreatedUtc", "UpdatedUtc"].
        /// </summary>
        public IReadOnlyList<string> ArrayOrderKeys { get; set; } = Array.Empty<string>();

        /// <summary>
        /// When <see cref="ArrayOrderKeys"/> is non-empty, arrays are normalized (sorted) BEFORE
        /// equality checks and diffing. Default: true.
        /// </summary>
        public bool NormalizeArraysBeforeDiff { get; set; } = true;

        /// <summary>
        /// When true, property lookups for array ordering are case-insensitive. Default: true.
        /// </summary>
        public bool CaseInsensitivePropertyLookup { get; set; } = true;

        /// <summary>
        /// When comparing property values for ordering, attempt to parse ISO-8601 datetimes
        /// from strings and compare them as <see cref="DateTimeOffset"/>. Default: true.
        /// </summary>
        public bool TreatStringsAsDateTimeWhenPossible { get; set; } = true;

        /// <summary>
        /// When DiffArrays=true, visit at most this many elements per array. Default: 256.
        /// </summary>
        public int MaxArrayItems { get; set; } = 256;

        /// <summary>
        /// Optional allow/deny filters. If provided, a path must satisfy allow (if set) and must not match deny.
        /// </summary>
        public Func<string, bool>? AllowPath { get; set; }
        public Func<string, bool>? DenyPath { get; set; }

        /// <summary>Return true to redact this path in the emitted ChangeRecord (e.g., Data.Secrets.*, *.Password).</summary>
        public Func<string, bool>? RedactPath { get; init; }

        /// <summary>Transforms a value for redaction. Default: literal string "REDACTED".</summary>
        public Func<JsonNode?, JsonNode?>? RedactValue { get; init; }
    }
}
