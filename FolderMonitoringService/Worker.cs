using FolderMonitoringService.Interfaces;

namespace FolderMonitoringService
{
    public class Worker(ILogger<Worker> logger, IAppFileWatcher fileWatcher) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("starting file system monitoring as per configurations");
            fileWatcher.Start();
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            fileWatcher.Stop();
            await base.StopAsync(stoppingToken);
        }

    }
}
