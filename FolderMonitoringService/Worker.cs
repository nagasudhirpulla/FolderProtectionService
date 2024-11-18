using FolderMonitoringService.Interfaces;

namespace FolderMonitoringService
{
    public class Worker(ILogger<Worker> logger, IAppFileWatcher fileWatcher) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("starting file system monitoring as per configurations");
            await fileWatcher.StartAsync(stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await fileWatcher.Stop(stoppingToken);
            await base.StopAsync(stoppingToken);
        }

    }
}
