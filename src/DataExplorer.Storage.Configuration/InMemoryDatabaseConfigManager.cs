#nullable enable
using Cloudbrick.DataExplorer.Storage.Configuration;
using System.Collections.Concurrent;

namespace Cloudbrick.DataExplorer.Storage.Configuration;

using Cloudbrick.DataExplorer.Storage.Abstractions;

public sealed class InMemoryDatabaseConfigManager : IDatabaseConfigManager
{
    private readonly ConcurrentDictionary<string, DatabaseRegistration> _registrations =
        new(StringComparer.OrdinalIgnoreCase);

    public InMemoryDatabaseConfigManager(IEnumerable<DatabaseRegistration>? seedRegistrations = null)
    {
        if (seedRegistrations is not null)
        {
            foreach (var reg in seedRegistrations)
            {
                // last-in wins on duplicate DatabaseId
                AddOrUpdateAsync(reg).GetAwaiter().GetResult();
            }
        }
    }
    public Task AddOrUpdateAsync(DatabaseRegistration registration, CancellationToken ct = default)
    {
        _registrations[registration.DatabaseId] = registration;
        return Task.CompletedTask;
    }

    public Task<bool> RemoveAsync(string databaseId, CancellationToken ct = default)
        => Task.FromResult(_registrations.TryRemove(databaseId, out _));

    public Task<DatabaseRegistration?> GetAsync(string databaseId, CancellationToken ct = default)
        => Task.FromResult(_registrations.TryGetValue(databaseId, out var reg) ? reg : null);

    public Task<IReadOnlyList<DatabaseRegistration>> ListAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<DatabaseRegistration>>(_registrations.Values.ToList());
}
