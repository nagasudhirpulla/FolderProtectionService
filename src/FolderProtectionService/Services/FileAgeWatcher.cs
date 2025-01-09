using FolderProtectionService.Config;
using FolderProtectionService.Interfaces;
using Quartz.Util;
using Quartz;

namespace FolderProtectionService.Services;

/// <summary>
/// Sets up and manages cron jobs as per configured periodicity 
/// to delete the files in configured folders if they are violating file age constraints
/// </summary>
/// <param name="logger"></param>
/// <param name="folderConfigsService"></param>
/// <param name="schedulerFactory"></param>
public class FileAgeWatcher(ILogger<FileAgeWatcher> logger, FolderConfigsService folderConfigsService, ISchedulerFactory schedulerFactory) : IFilesMonitorService
{
    private readonly List<FolderMonitorConfig> FolderMonitorConfigs = folderConfigsService.FolderMonitorConfigs;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // configure file age monitoring schedulers
        logger.LogInformation("Setting up file age monitoring schedulers");
        foreach (FolderMonitorConfig folderConfig in folderConfigsService.FolderMonitorConfigs)
        {
            if (folderConfig.MaxAgeDays > 0 && !folderConfig.FolderCheckCron.IsNullOrWhiteSpace())
            {
                string cronExpr = folderConfig.FolderCheckCron;
                try
                {
                    _ = new CronExpression(cronExpr);
                }
                catch (FormatException e)
                {
                    logger.LogError($"Invalid CRON expression provided - {cronExpr} with error {e.Message}");
                    continue;
                }

                var trigger = TriggerBuilder.Create()
                            .WithCronSchedule(cronExpr)
                            .Build();
                var job = JobBuilder.Create<FilesAgeCheckJob>()
                           .Build();
                job.JobDataMap[nameof(FolderMonitorConfig)] = folderConfig;
                var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
                await scheduler.ScheduleJob(job, trigger, cancellationToken);
            }
        }
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        // stop all file age monitoring schedulers
        logger.LogInformation("Stopping file age monitoring schedulers");
        foreach (var sch in await schedulerFactory.GetAllSchedulers(cancellationToken))
        {
            await sch.Shutdown(cancellationToken);
        }
    }
}
