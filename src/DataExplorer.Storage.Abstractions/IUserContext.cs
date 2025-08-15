#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IUserContext
{
    string PrincipalId { get; }
    string? Name { get; }
    IReadOnlyDictionary<string, string>? Claims { get; }
}
