#nullable enable

using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable



namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// Represents the identity of the user performing an operation.
/// </summary>
/// <remarks>
/// Implementations expose principal information and optional claims for authorization and auditing.
/// </remarks>
public interface IUserContext
{
    /// <summary>
    /// Gets the unique identifier of the principal.
    /// </summary>
    string PrincipalId { get; }

    /// <summary>
    /// Gets the display name of the user, if available.
    /// </summary>
    /// <remarks>
    /// May be <c>null</c> when the name is not known.
    /// </remarks>
    string? Name { get; }

    /// <summary>
    /// Gets additional claims associated with the user.
    /// </summary>
    /// <remarks>
    /// Returns <c>null</c> when no claims are provided.
    /// </remarks>
    IReadOnlyDictionary<string, string>? Claims { get; }
}
