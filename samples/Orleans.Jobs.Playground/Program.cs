using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Streams;
using Orleans.Serialization;
using Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;
using Cloudbrick.Orleans.Jobs.Abstractions;
using Cloudbrick.Orleans.Jobs.Abstractions.Enums;
using Cloudbrick.Orleans.Jobs.Executors;
using Cloudbrick.Orleans.Jobs.Telemetry;

class Program
{
    static async Task Main(string[] args)
    {
        using var host = await StartSiloAsync();

        // Create a client to the in-proc silo
        //var client = await new ClientBuilder()
        //    .UseLocalhostClustering()
        //    .AddSimpleMessageStreamProvider(StreamConstants.ProviderName)
        //    .Build()
        //    .Connect();

        try
        {
            var client = host.Services.GetRequiredService<IClusterClient>();
            var jobs = client.GetGrain<IJobsManagerGrain>("manager");

            var spec = new JobSpec
            {
                Name = "playground-job",
                MaxDegreeOfParallelism = 2,
                FailFast = true,
                CorrelationId = Guid.NewGuid().ToString("N"),
                TelemetryProviderKey = "nope"
            };
            spec.Tasks["adder"] = new TaskSpec { ExecutorType = "adder", CommandJson = "{ \"a\": 40, \"b\": 2 }" };
            spec.Tasks["wait"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 1000, \"steps\": 10 }", Dependencies = { "adder" } };
            spec.Tasks["wait2"] = new TaskSpec { ExecutorType = "delay", CommandJson = "{ \"milliseconds\": 1000, \"steps\": 10 }", Dependencies = { "adder" , "wait" } };

            var jobId = await jobs.CreateJobAsync(spec);

            // Subscribe to job telemetry
            var provider = client.GetStreamProvider(StreamConstants.ProviderName);
            var jobStream = provider.GetStream<ExecutionEvent>(StreamId.Create(StreamConstants.JobNamespace, jobId.ToString()));
            await jobStream.SubscribeAsync((evt, ct) =>
            {
                Console.WriteLine($"[JOB {evt.JobId}][{evt.CorrelationId}] {evt.EventType} {evt.TaskId} {evt.Message} { (evt.Progress.HasValue ? $"{evt.Progress}%" : "") }");
                return Task.CompletedTask;
            });

            var job = client.GetGrain<IJobGrain>(jobId);
            await job.EmitTelemetryAsync(new ExecutionEvent { EventType = ExecutionEventType.Custom, Message = "Manual telemetry before start" });
            await job.FlushAsync();

            Console.WriteLine("Starting job...");
            await jobs.StartJobAsync(jobId);

            // Wait until job completes
            while (true)
            {
                var state = await jobs.GetJobStateAsync(jobId);
                if (state.Status is JobStatus.Succeeded
                    or JobStatus.Failed
                    or JobStatus.Cancelled)
                {
                    Console.WriteLine($"Job finished: {state.Status}");
                    break;
                }
                await Task.Delay(200);
            }
        }
        finally
        {
            //await client.Close();
            await host.StopAsync();
        }
    }
    private static async Task<IHost> StartSiloAsync()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureLogging(lb => lb.AddConsole())
            .UseOrleans(silo =>
            {
                silo.UseLocalhostClustering()
                    .AddMemoryGrainStorageAsDefault()
                    .AddMemoryStreams(StreamConstants.ProviderName)
                    .AddMemoryGrainStorage(StreamConstants.ProviderName);

                //.ConfigureApplicationParts(parts => parts
                //    .AddApplicationPart(typeof(TaskGrain).Assembly)
                //    .WithCodeGeneration());

                silo.Services.AddSerializer(serializerBuilder => serializerBuilder.AddNewtonsoftJsonSerializer(type => true));
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<ITaskExecutor, DelayExecutor>();
                services.AddSingleton<ITaskExecutor, AdderExecutor>();
                services.AddSingleton<ITaskExecutorFactory, ExecutorFactory>();
                services.AddSingleton<ITelemetrySinkFactory, TelemetrySinkFactory>();
            })
            .Build();

        await host.StartAsync();
        return host;
    }
}
