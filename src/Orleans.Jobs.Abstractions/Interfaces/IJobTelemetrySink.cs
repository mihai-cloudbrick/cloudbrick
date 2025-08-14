using System.Threading.Tasks;
using Cloudbrick.Orleans.Jobs.Abstractions.Models;

namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

public interface IJobTelemetrySink
{
    Task OnJobEventAsync(ExecutionEvent evt);
    Task OnTaskEventAsync(ExecutionEvent evt);
}
