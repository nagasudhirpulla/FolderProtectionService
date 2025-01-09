namespace FolderProtectionService.Interfaces;

/// <summary>
/// Interface to be implemented by all the folder monitoring services
/// </summary>
public interface IFilesMonitorService
{
    public Task StartAsync(CancellationToken cancellationToken);
    public Task Stop(CancellationToken cancellationToken);
}