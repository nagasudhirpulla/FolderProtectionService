using FolderProtectionService.Config;
using Quartz;

namespace FolderProtectionService.Services;

/// <summary>
/// A cron job script that deletes old files from a folder as per supplied configuration
/// </summary>
/// <param name="filesService"></param>
/// <param name="logger"></param>
public class FilesAgeCheckJob(FilesService filesService, ILogger<FilesAgeCheckJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        // checks all files / folders of a folder for old files and delete them
        logger.LogDebug($"Started file age checking job at {DateTime.Now}");
        FolderMonitorConfig folderConfig = context.JobDetail.JobDataMap[nameof(FolderMonitorConfig)] as FolderMonitorConfig ?? throw new Exception("folder monitoring config not provided for job");
        ProcessDirectory(folderConfig.FolderPath, folderConfig);
        logger.LogDebug($"Ended file age checking job at {DateTime.Now}");
        await Task.FromResult(0);
    }

    public void ProcessDirectory(string targetDirectory, FolderMonitorConfig folderConfig)
    {
        // Process the list of files found in the directory.
        foreach (string fileName in Directory.EnumerateFiles(targetDirectory))
        {
            // check if file is whitelisted
            if (FilesService.CheckIfFileWhitelisted(folderConfig, fileName))
            {
                logger.LogWarning($"Skipping whitelisted file {Path.GetFileName(fileName)}");
                return;
            }
            _ = filesService.DeleteFileIfOld(fileName, folderConfig); 
        }

        // Recurse into subdirectories of this directory.
        if (folderConfig.IncludeSubFolders)
        {
            foreach (string subdirectory in Directory.EnumerateDirectories(targetDirectory))
                ProcessDirectory(subdirectory, folderConfig);
        }
    }
}