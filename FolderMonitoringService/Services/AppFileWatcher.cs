using FolderMonitoringService.Config;
using FolderMonitoringService.Interfaces;
using Microsoft.Extensions.Logging;

namespace FolderMonitoringService.Services
{
    public class AppFileWatcher(ILogger<Worker> logger, IConfiguration configuration) : IAppFileWatcher
    {
        private readonly List<FileSystemWatcher> FolderWatchers = [];
        private readonly List<FolderMonitorConfig> FolderMonitorConfigs = configuration.GetSection("Folders").Get<List<FolderMonitorConfig>>() ?? [];
        private const int _maxRetries = 10;

        public void Start()
        {
            // Start the file system watcher for each of the file specification
            StartFileSystemWatcher();
        }
        private void StartFileSystemWatcher()
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

                // Associate the event that will be triggered when a new file
                // is added to the monitored folder, using a lambda expression

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
                // check if initial scan required
                if (!folderConfig.InitialScan)
                {
                    continue;
                }
                var folderPath = folderConfig.FolderPath;

                // check if directory is a valid
                if (!Directory.Exists(folderPath))
                {
                    continue;
                }

                logger.LogInformation(message: $"Starting initial scan of folder {folderPath}");

                // process directory
                ProcessDirectory(folderPath, folderConfig);
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
            //logger.LogWarning($"{e.ChangeType}");
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
            if ((allowedExtsns.Count > 0) && !allowedExtsns.Contains(extsn))
            {
                DeleteFile(filePath, $"file extension {extsn} not allowed");
                return true;
            }

            // delete if file size is more
            var maxAllowedFileSize = folderConfig.MaxFileSizeMb;
            double fileSizeMb = (double)new FileInfo(filePath).Length / (double)(1024 * 1024);
            if ((maxAllowedFileSize > 0) && (fileSizeMb > maxAllowedFileSize))
            {
                DeleteFile(filePath, $"file size {fileSizeMb}MB is more than {maxAllowedFileSize}MB");
                return true;
            }
            return false;
        }

        private void DeleteFile(string filePath, string deleteMsg)
        {
            bool isFileUnLocked = WaitForFile(filePath);
            if (isFileUnLocked)
            {
                File.Delete(filePath);
                logger.LogWarning($"{filePath} deleted, {deleteMsg}");
            }
        }

        private void OnFileWatcherError(object sender, ErrorEventArgs e)
        {
            logger.LogError($"File error event {e.GetException().Message}");
        }

        public void ProcessDirectory(string targetDirectory, FolderMonitorConfig folderConfig)
        {
            // Process the list of files found in the directory.
            foreach (string fileName in Directory.EnumerateFiles(targetDirectory))
                _ = DeleteFileIfViolating(fileName, folderConfig);

            // Recurse into subdirectories of this directory.
            if (folderConfig.IncludeSubFolders)
            {
                foreach (string subdirectory in Directory.EnumerateDirectories(targetDirectory))
                    ProcessDirectory(subdirectory, folderConfig);
            }
        }

        private bool WaitForFile(string fullPath)
        {
            int numTries = 0;
            while (true)
            {
                ++numTries;
                try
                {
                    // Attempt to open the file exclusively.
                    using (FileStream fs = new FileStream(fullPath,
                        FileMode.Open, FileAccess.ReadWrite,
                        FileShare.None, 100))
                    {
                        fs.ReadByte();
                        // If we got this far the file is ready
                        break;
                    }
                }
                catch (Exception ex)
                {
                    //logger.LogWarning(
                    //   "WaitForFile {0} failed to get an exclusive lock: {1}",
                    //    fullPath, ex.ToString());

                    if (numTries > _maxRetries)
                    {
                        logger.LogWarning(
                            "WaitForFile {0} giving up after 10 tries",
                            fullPath);
                        return false;
                    }

                    // Wait for the lock to be released
                    Thread.Sleep(500);
                }
            }

            logger.LogTrace("WaitForFile {0} returning true after {1} tries",
                fullPath, numTries);
            return true;
        }

    }
}
