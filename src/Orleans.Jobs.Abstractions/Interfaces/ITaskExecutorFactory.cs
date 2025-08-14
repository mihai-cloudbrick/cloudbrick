namespace Cloudbrick.Orleans.Jobs.Abstractions.Interfaces;

public interface ITaskExecutorFactory
{
    ITaskExecutor Resolve(string executorType);
}
