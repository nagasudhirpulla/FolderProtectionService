using FolderMonitoringService.Interfaces;
using FolderMonitoringService.Services;

namespace FolderMonitoringService
{
    public class Worker(ILogger<Worker> logger, IFilesMonitorService fileWatcher, FileAgeWatcher fileAgeWatcher) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("starting file system monitoring as per configurations");
            await fileWatcher.StartAsync(stoppingToken);
            await fileAgeWatcher.StartAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await fileWatcher.Stop(stoppingToken);            
            await fileAgeWatcher.Stop(stoppingToken);

            await base.StopAsync(stoppingToken);
        }

    }
}
