using System;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

[Flags]
public enum TableCapabilityFlags
{
    None = 0,
    ServerSideQuery = 1 << 0,
    ContinuationPaging = 1 << 1,
    ServerSideProjection = 1 << 2,
    ServerSideAggregation = 1 << 3,
    ParameterizedQuery = 1 << 4,
    StrongConsistency = 1 << 5
}
