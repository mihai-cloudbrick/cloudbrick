using Microsoft.AspNetCore.Http;

namespace Cloudbrick.Orleans.SignalR;

public sealed class HubRouting
{
    public string Resolve(HttpContext ctx)
    {
        if (ctx.Request.Query.TryGetValue("hub", out var q) && !string.IsNullOrWhiteSpace(q))
            return q!;
        if (ctx.Request.Headers.TryGetValue("x-hub", out var h) && !string.IsNullOrWhiteSpace(h))
            return h!;
        return "default";
    }

    public string HubGroup(string hub)              => $"hub::{hub}";
    public string UserGroup(string hub, string uid) => $"hub::{hub}::user::{uid}";
    public string Group(string hub, string grp)     => $"hub::{hub}::grp::{grp}";
}
