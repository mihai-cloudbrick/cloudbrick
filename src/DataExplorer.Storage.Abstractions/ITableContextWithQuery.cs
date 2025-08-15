#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Represents a table context that also supports querying.
/// </summary>
/// <remarks>
/// Combines CRUD operations from <see cref="ITableContext"/> with query capabilities defined by <see cref="ITableQuery"/>.
/// </remarks>
public interface ITableContextWithQuery : ITableContext, ITableQuery { }
