using FolderProtectionService.Config;
using FolderProtectionService.Interfaces;

namespace FolderProtectionService.Services;

/// <summary>
/// Watches for changes in configured folders and deletes the files if they are violating constraints
/// </summary>
/// <param name="logger"></param>
/// <param name="filesService"></param>
/// <param name="folderConfigsService"></param>
public class FileChangeWatcher(ILogger<FileChangeWatcher> logger, FilesService filesService, FolderConfigsService folderConfigsService) : IFilesMonitorService
{
    private readonly List<FileSystemWatcher> FolderWatchers = [];
    private readonly List<FolderMonitorConfig> FolderMonitorConfigs = folderConfigsService.FolderMonitorConfigs;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Start the file system watcher for each of the file specification
        StartFileSystemWatcher(cancellationToken);
        await Task.FromResult(0);
    }
    private void StartFileSystemWatcher(CancellationToken cancellationToken)
    {
        // Create folder watcher for each configuration
        foreach (FolderMonitorConfig folderConfig in FolderMonitorConfigs)
        {
            // Creates a new instance of FileSystemWatcher
            FileSystemWatcher folderWatch = new();

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
        await Task.FromResult(0);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e, FolderMonitorConfig folderConfig)
    {
        //logger.LogWarning($"{e.ChangeType}");
        // check if file is whitelisted
        if (FilesService.CheckIfFileWhitelisted(folderConfig, e.FullPath))
        {
            logger.LogWarning($"Skipping whitelisted file {Path.GetFileName(e.FullPath)}");
            return;
        }
        _ = filesService.DeleteFileIfViolating(e.FullPath, folderConfig);
    }

    private void OnFileWatcherError(object sender, ErrorEventArgs e)
    {
        logger.LogError($"File error event {e.GetException().Message}");
    }
}
