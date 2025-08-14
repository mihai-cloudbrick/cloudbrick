#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface ITableContextWithQuery : ITableContext, ITableQuery { }
