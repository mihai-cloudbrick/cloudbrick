#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Provider.Sql;

public sealed class SqlOptions : ProviderOptionsBase
{
    public override StorageProviderKind Kind => StorageProviderKind.SqlDatabase;

    public required string ConnectionString { get; set; }
    public string Schema { get; set; } = "dbo";
}
