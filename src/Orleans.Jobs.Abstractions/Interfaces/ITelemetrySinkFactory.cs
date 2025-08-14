using System;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

public interface ITelemetrySinkFactory
{
    IJobTelemetrySink Create(string providerKey, Guid jobId, string correlationId);
}
