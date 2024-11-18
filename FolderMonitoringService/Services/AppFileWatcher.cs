using FolderMonitoringService.Config;
using FolderMonitoringService.Interfaces;
using Quartz;
using Quartz.Util;

namespace FolderMonitoringService.Services;

public class AppFileWatcher(ILogger<AppFileWatcher> logger, IConfiguration configuration, ISchedulerFactory schedulerFactory, FilesService filesService) : IAppFileWatcher
{
    private readonly List<FileSystemWatcher> FolderWatchers = [];
    private readonly List<FolderMonitorConfig> FolderMonitorConfigs = configuration.GetSection("Folders").Get<List<FolderMonitorConfig>>() ?? [];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Start the file system watcher for each of the file specification
        await StartFileSystemWatcherAsync(cancellationToken);
    }
    private async Task StartFileSystemWatcherAsync(CancellationToken cancellationToken)
    {
        // Create folder watcher for each configuration
        foreach (FolderMonitorConfig folderConfig in FolderMonitorConfigs)
        {
            DirectoryInfo dir = new(folderConfig.FolderPath);

            // check if directory is valid
            if (!dir.Exists)
            {
                logger.LogError($"directory not found at {folderConfig.FolderPath}");
                continue;
            }

            // Checks whether the folder is enabled
            if (!folderConfig.Enabled)
            {
                logger.LogError($"directory monitoring disabled for {folderConfig.FolderPath}");
                continue;
            }

            // Creates a new instance of FileSystemWatcher
            FileSystemWatcher folderWatch = new();

            // clean allowed extensions
            folderConfig.AllowedExtensions = folderConfig.AllowedExtensions.Select(e =>
            {
                string extnsStr = e.ToLower();
                if (!extnsStr.StartsWith('.'))
                {
                    extnsStr = "." + extnsStr;
                }
                return extnsStr;
            }).ToList();

            // Folder location to monitor
            folderWatch.Path = folderConfig.FolderPath;

            // Subscribe to notify filters
            folderWatch.NotifyFilter = NotifyFilters.FileName
                         | NotifyFilters.Size;

            //folderWatch.Created += (senderObj, fileSysArgs) =>
            //  OnFileChanged(senderObj, fileSysArgs, folderConfig);

            folderWatch.Changed += (senderObj, fileSysArgs) =>
              OnFileChanged(senderObj, fileSysArgs, folderConfig);

            folderWatch.Renamed += (senderObj, fileSysArgs) =>
              OnFileChanged(senderObj, fileSysArgs, folderConfig);

            folderWatch.Error += OnFileWatcherError;

            // Begin watching
            folderWatch.EnableRaisingEvents = true;

            // enable monitoring in sub folders also
            folderWatch.IncludeSubdirectories = folderConfig.IncludeSubFolders;

            // Add the systemWatcher to the list
            FolderWatchers.Add(folderWatch);

            // Record a log entry into Windows Event Log
            logger.LogInformation(message: $"Starting to monitor files for allowed extensions {string.Join(", ", folderConfig.AllowedExtensions)} in the folder {folderWatch.Path}");

            // configure file age monitoring schedulers
            if (folderConfig.MaxAgeDays > 0 && !folderConfig.AgeCheckCron.IsNullOrWhiteSpace())
            {
                var trigger = TriggerBuilder.Create()
                            .WithCronSchedule(folderConfig.AgeCheckCron)
                            .Build();
                var job = JobBuilder.Create<FilesAgeCheckJob>()
                           .Build();
                job.JobDataMap[nameof(FolderMonitorConfig)] = folderConfig;
                var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
                await scheduler.ScheduleJob(job, trigger, cancellationToken);
            }
        }

        // perform initial folder scan if required
        foreach (var folderConfig in FolderMonitorConfigs)
        {
            //check if config is enabled
            if (!folderConfig.Enabled) { continue; }

            // check if initial scan required
            if (!folderConfig.InitialScan) { continue; }

            var folderPath = folderConfig.FolderPath;

            // check if directory is a valid
            if (!Directory.Exists(folderPath)) { continue; }

            logger.LogInformation(message: $"Starting initial scan of folder {folderPath}");

            // process directory
            filesService.ProcessDirectory(folderPath, folderConfig);
        }
    }

    public async Task Stop(CancellationToken cancellationToken)
    {
        foreach (FileSystemWatcher fsw in FolderWatchers)
        {
            // Stop listening
            fsw.EnableRaisingEvents = false;
            // Dispose the Object
            fsw.Dispose();
        }
        // Clean the list
        FolderWatchers.Clear();

        // stop all schedulers
        foreach (var sch in await schedulerFactory.GetAllSchedulers(cancellationToken))
        {
            await sch.Shutdown(cancellationToken);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e, FolderMonitorConfig folderConfig)
    {
        //logger.LogWarning($"{e.ChangeType}");
        _ = filesService.DeleteFileIfViolating(e.FullPath, folderConfig);
    }

    private void OnFileWatcherError(object sender, ErrorEventArgs e)
    {
        logger.LogError($"File error event {e.GetException().Message}");
    }
}
