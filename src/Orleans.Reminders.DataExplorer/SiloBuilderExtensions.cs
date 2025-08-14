#nullable enable
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace Cloudbrick.Orleans.Reminders.DataExplorer;

public static class SiloBuilderExtensions
{
    public static ISiloBuilder UseResourceManagerReminders(this ISiloBuilder builder, Action<DataExplorerReminderOptions> configure)
    { 
        builder.Services.Configure(configure); 
        builder.Services.AddSingleton<IReminderTable, DataExplorerReminderTable>(); 
        return builder; 
    }
}
