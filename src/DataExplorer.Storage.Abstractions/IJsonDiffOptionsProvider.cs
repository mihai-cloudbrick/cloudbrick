#nullable enable

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Provides access to <see cref="JsonDiffOptions"/> for individual databases.
/// </summary>
/// <remarks>
/// Implementations may retrieve configuration from storage or compute defaults. An exception is thrown when no configuration is available.
/// </remarks>
public interface IJsonDiffOptionsProvider
{
    /// <summary>
    /// Gets diff options for the specified database.
    /// </summary>
    /// <param name="databaseId">The identifier of the database.</param>
    /// <returns>The diff options associated with the database.</returns>
    /// <remarks>
    /// Throws if <paramref name="databaseId"/> is unknown or configuration retrieval fails.
    /// </remarks>
    JsonDiffOptions GetForDatabase(string databaseId);
}
