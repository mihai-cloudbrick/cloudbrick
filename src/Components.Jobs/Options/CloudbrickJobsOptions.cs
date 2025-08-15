namespace Cloudbrick.Components.Jobs.Options
{
    public class CloudbrickJobsOptions
    {
        public string ApiBaseUrl { get; set; } = "/api/jobs";
        public string TelemetryHubUrl { get; set; } = "/hubs/telemetry";
        public int GridRefreshIntervalMs { get; set; } = 2000;
    }
}
