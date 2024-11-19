using FolderMonitoringService.Config;

namespace FolderMonitoringService.Services;

public class FilesService(ILogger<Worker> logger)
{
    private const int _maxRetries = 10;
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

    public bool DeleteFileIfViolating(string filePath, FolderMonitorConfig folderConfig)
    {
        // delete if file extension is not valid
        string extsn = Path.GetExtension(filePath);
        List<string> allowedExtsns = folderConfig.AllowedExtensions;
        if ((allowedExtsns.Count > 0) && !allowedExtsns.Contains(extsn))
        {
            _ = DeleteFile(filePath, $"file extension {extsn} not allowed");
            return true;
        }

        // delete if file size is more
        var maxAllowedFileSize = folderConfig.MaxFileSizeMb;
        double fileSizeMb = (double)new FileInfo(filePath).Length / (double)(1024 * 1024);
        if ((maxAllowedFileSize > 0) && (fileSizeMb > maxAllowedFileSize))
        {
            _ = DeleteFile(filePath, $"file size {fileSizeMb}MB is more than {maxAllowedFileSize}MB");
            return true;
        }

        // delete file if old
        bool isFileDeleted = DeleteFileIfOld(filePath, folderConfig);

        return isFileDeleted;
    }

    public bool DeleteFileIfOld(string filePath, FolderMonitorConfig folderConfig)
    {
        float maxAgeDays = folderConfig.MaxAgeDays;
        if (IsFileOld(filePath, maxAgeDays))
        {
            _ = DeleteFile(filePath, $"file is older than {maxAgeDays} days");
            return true;
        }
        return false;
    }

    private static bool IsFileOld(string filePath, float maxAgeDays)
    {
        if (maxAgeDays <= 0) { return false; }
        DateTime fileCreatedAt = File.GetCreationTime(filePath);
        DateTime fileModifiedAt = File.GetLastWriteTime(filePath);
        var fileTime = new DateTime(Math.Min(fileCreatedAt.Ticks, fileModifiedAt.Ticks));
        if (DateTime.Now - fileTime > TimeSpan.FromDays(maxAgeDays))
        {
            return true;
        }
        return false;
    }

    private bool DeleteFile(string filePath, string deleteMsg)
    {
        bool isFileUnLocked = WaitForFile(filePath);
        if (isFileUnLocked)
        {
            File.Delete(filePath);
            logger.LogWarning($"{filePath} deleted, {deleteMsg}");
            return true;
        }
        else
        {
            logger.LogError($"Unable to delete {filePath} since file was locked, {deleteMsg}");
            return false;
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
            catch (Exception)
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
