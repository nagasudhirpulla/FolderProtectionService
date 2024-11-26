namespace FolderProtectionService.Interfaces;

public interface IFilesMonitorService
{
    public Task StartAsync(CancellationToken cancellationToken);
    public Task Stop(CancellationToken cancellationToken);
}