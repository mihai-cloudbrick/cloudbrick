using Microsoft.AspNetCore.SignalR;

namespace Cloudbrick.Orleans.SignalR;

// Optional: scope user ids by hub to avoid cross-hub user broadcasts if you use Clients.User(...).
public sealed class HubScopedUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var baseId = connection.User?.Identity?.Name ?? connection.UserIdentifier;
        if (string.IsNullOrWhiteSpace(baseId)) return null;

        var http = connection.GetHttpContext();
        var hub = "default";
        if (http != null)
        {
            if (http.Request.Query.TryGetValue("hub", out var q) && !string.IsNullOrWhiteSpace(q)) hub = q!;
            else if (http.Request.Headers.TryGetValue("x-hub", out var h) && !string.IsNullOrWhiteSpace(h)) hub = h!;
        }
        return $"{hub}::{baseId}";
    }
}
