#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

/// <summary>
/// A single database registration: a database id bound to strongly-typed provider options.
/// </summary>
public sealed record DatabaseRegistration(string DatabaseId, IProviderOptions Options);
