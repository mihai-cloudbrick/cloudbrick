using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cloudbrick.Orleans.SignalR
{
    public static class SiloExtensions
    {
        public static ISiloBuilder AddCloudbrickSignalRInMemory(this ISiloBuilder builder)
        {
            builder.AddMemoryStreams(HubConstants.ProviderName);
            builder.AddMemoryGrainStorage(HubConstants.ProviderName);

            builder.Services.AddCloudbrickSignalRCore();
            return builder;
        }

        private static IServiceCollection AddCloudbrickSignalRCore(this IServiceCollection services)
        {
            services.AddSingleton<HubRouting>();
            services.AddSingleton<IHubSubscriptionCoordinator, HubRelayHostedService>();
            services.AddHostedService(sp => (HubRelayHostedService)sp.GetRequiredService<IHubSubscriptionCoordinator>());
            services.AddSingleton<IHubMessageSender, HubMessageSender>();
            services.AddSingleton<IUserIdProvider, HubScopedUserIdProvider>();
            return services;
        }

        public static WebApplication MapCloudbrickSignalR(this WebApplication app)
        {

            app.MapHub<DynamicHub>($"/_hubs");
            // Convenience APIs
            app.MapPost("/_hubs/realtime/{hub}/all/{method}", async (string hub, string method, IHubMessageSender sender, object body) =>
            {
                await sender.ToAll(hub, method, body);
                return Results.Accepted();
            });
            app.MapPost("/_hubs/{hub}/group/{group}/{method}", async (string hub, string group, string method, IHubMessageSender sender, object body) =>
            {
                await sender.ToGroup(hub, group, method, body);
                return Results.Accepted();
            });
            app.MapPost("/_hubs/{hub}/user/{userId}/{method}", async (string hub, string userId, string method, IHubMessageSender sender, object body) =>
            {
                await sender.ToUser(hub, userId, method, body);
                return Results.Accepted();
            });

            return app;
        }
    }
}
