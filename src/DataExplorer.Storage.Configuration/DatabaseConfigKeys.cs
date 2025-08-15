#nullable enable

using Cloudbrick.DataExplorer.Storage.Configuration;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

public static class DatabaseConfigKeys
{
    // Local File System
    public const string LocalRoot = "Local.Root";
    public const string LocalShardDepth = "Local.ShardDepth";
    public const string LocalShardWidth = "Local.ShardWidth";

    // Azure Blob
    public const string BlobConnectionString = "Blob.ConnectionString";
    public const string BlobPublicAccess = "Blob.PublicAccess";
    public const string BlobShardDepth = "Blob.ShardDepth";
    public const string BlobShardWidth = "Blob.ShardWidth";

    // Azure Table
    public const string TableConnectionString = "Table.ConnectionString";

    // Cosmos DB
    public const string CosmosEndpoint = "Cosmos.Endpoint";
    public const string CosmosKey = "Cosmos.Key";
    public const string CosmosPartitionKey = "Cosmos.PartitionKey";
    public const string CosmosConsistency = "Cosmos.Consistency";

    // SQL
    public const string SqlConnectionString = "Sql.ConnectionString";
    public const string SqlSchema = "Sql.Schema";

    // JsonDiff (per-database)
    public const string DiffRootPath = "Diff.RootPath"; // default: Data
    public const string DiffMaxChanges = "Diff.MaxChanges";
    public const string DiffMaxDepth = "Diff.MaxDepth";
    public const string DiffArrayOrderKeys = "Diff.ArrayOrderKeys"; // comma-separated
    public const string DiffNormalizeArrays = "Diff.NormalizeArrays"; // bool
    public const string DiffDiffArrays = "Diff.DiffArrays"; // bool
    public const string DiffCaseInsensitivePropertyLookup = "Diff.CaseInsensitivePropertyLookup"; // bool
    public const string DiffTreatStringsAsDateTime = "Diff.TreatStringsAsDateTime"; // bool
    public const string DiffMaxArrayItems = "Diff.MaxArrayItems";
}
