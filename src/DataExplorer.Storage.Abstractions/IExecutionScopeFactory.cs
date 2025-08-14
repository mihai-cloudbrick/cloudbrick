#nullable enable
using Cloudbrick.DataExplorer.Storage.Abstractions;

#nullable enable
using Microsoft.Extensions.Logging;

namespace Cloudbrick.DataExplorer.Storage.Abstractions;

public interface IExecutionScopeFactory
{
    IDisposable Begin(ILogger logger, IExecutionContext ctx);
}
