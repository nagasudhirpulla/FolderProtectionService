using FolderMonitoringService.Config;
using FolderMonitoringService.Interfaces;

namespace FolderMonitoringService.Services
{
    public class AppFileWatcher(ILogger<Worker> logger, IConfiguration configuration) : IAppFileWatcher
    {
        private readonly List<FileSystemWatcher> FolderWatchers = [];
        private readonly List<FolderMonitorConfig> FolderMonitorConfigs = configuration.GetSection("Folders").Get<List<FolderMonitorConfig>>() ?? [];

        public void Start()
        {
            // Start the file system watcher for each of the file specification
            StartFileSystemWatcher();
        }
        private void StartFileSystemWatcher()
        {
            // Loop the list to process each of the folder specifications found
            foreach (FolderMonitorConfig folderConfig in FolderMonitorConfigs)
            {
                DirectoryInfo dir = new(folderConfig.FolderPath);

                // check if directory is a valid
                if (!dir.Exists)
                {
                    logger.LogError($"directory not found at {folderConfig.FolderPath}");
                    continue;
                }

                // Checks whether the folder is enabled
                if (!folderConfig.IsEnabled)
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
                folderWatch.NotifyFilter = NotifyFilters.DirectoryName
                             | NotifyFilters.FileName
                             | NotifyFilters.Size;

                // Associate the event that will be triggered when a new file
                // is added to the monitored folder, using a lambda expression
                // TODO creation event not working
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
                folderWatch.IncludeSubdirectories = folderConfig.IsIncludeSubFolders;

                // Add the systemWatcher to the list
                FolderWatchers.Add(folderWatch);

                // Record a log entry into Windows Event Log
                logger.LogInformation(message: $"Starting to monitor files for allowed extensions {string.Join(", ", folderConfig.AllowedExtensions)} in the folder {folderWatch.Path}");
            }

        }

        public void Stop()
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
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e, FolderMonitorConfig folderConfig)
        {
            _ = DeleteFileIfViolating(e.FullPath, folderConfig);

        }

        private bool DeleteFileIfViolating(string filePath, FolderMonitorConfig folderConfig)
        {
            // check if file is valid
            bool isFile = File.Exists(filePath);
            if (!isFile)
            {
                return false;
            }

            // delete if file extension is not valid
            string extsn = Path.GetExtension(filePath);
            List<string> allowedExtsns = folderConfig.AllowedExtensions;
            if (!allowedExtsns.Contains(extsn))
            {
                DeleteFile(filePath, $"file extension {extsn} not allowed");
                return true;
            }

            // delete if file size is more
            var maxAllowedFileSize = folderConfig.MaxFileSizeMb;
            long fileSizeKb = new FileInfo(filePath).Length / 1024;
            if (fileSizeKb > maxAllowedFileSize)
            {
                DeleteFile(filePath, $"file size {fileSizeKb}KB is more than {maxAllowedFileSize}KB");
                return true;
            }
            return false;
        }

        private void DeleteFile(string filePath, string deleteMsg)
        {
            File.Delete(filePath);
            logger.LogInformation($"{filePath} deleted, {deleteMsg}");

        }

        private void OnFileWatcherError(object sender, ErrorEventArgs e)
        {
            logger.LogInformation($"File error event {e.GetException().Message}");
        }

    }
}
