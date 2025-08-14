#nullable enable

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IJsonDiffOptionsProvider
{
    JsonDiffOptions GetForDatabase(string databaseId);
}
