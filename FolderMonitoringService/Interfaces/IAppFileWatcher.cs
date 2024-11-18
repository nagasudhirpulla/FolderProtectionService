namespace FolderMonitoringService.Interfaces;

public interface IAppFileWatcher
{
    public Task StartAsync(CancellationToken cancellationToken);
    public Task Stop(CancellationToken cancellationToken);
}