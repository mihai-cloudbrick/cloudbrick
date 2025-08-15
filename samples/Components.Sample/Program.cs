using Components.Sample.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Cloudbrick.Components.Jobs;
using Cloudbrick.Components.Blades.Extensions;
using Components.Sample.Components.Blades.Components;
using Orleans.Hosting;
using Cloudbrick.Orleans.Jobs;
using Cloudbrick.Orleans.SignalR;
using Swashbuckle.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();
builder.Services.AddCloudbrickJobsComponents(opts =>
{
    opts.ApiBaseUrl = "https://localhost:7206/api/jobs";        // your REST endpoints
    opts.TelemetryHubUrl = "https://localhost:7206/_hubs/telemetry"; // SignalR hub for telemetry
});

builder.UseOrleans(c =>
{
    c.UseLocalhostClustering();
    c.AddCloudbrickJobsInMemory();
    c.AddCloudbrickSignalRInMemory();
});

builder.Services.AddCloudbrickBlades(reg =>
{
    reg.Add<SubscriptionBlade>("Portal.Subscription");
    reg.Add<ResourceGroupBlade>("Portal.ResourceGroup");
    reg.Add<ResourceTypeListBlade>("Provider.Type.List");
    reg.Add<ResourceDetailsBlade>("Provider.Resource.Details");
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

   
}

app.UseSwagger();
app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = "_docs";
});

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();

app.MapCloudbrickSignalR();
app.MapCloudbrickJobsTelemetryHub();
//app.MapHub<TelemetryHub>("/hubs/telemetry");
app.Run();
