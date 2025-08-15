namespace Cloudbrick.Orleans.SignalR;

public interface IHubSubscriptionCoordinator
{
    Task EnsureSubscribed(string hub);
}
