using FolderMonitoringService.Config;

namespace FolderMonitoringService.Services;

public class FolderConfigsService(ILogger<FolderConfigsService> logger, IConfiguration configuration)
{
    public readonly List<FolderMonitorConfig> FolderMonitorConfigs = GetConfigs(logger, configuration);

    private static List<FolderMonitorConfig> GetConfigs(ILogger<FolderConfigsService> logger, IConfiguration configuration)
    {
        List<FolderMonitorConfig> folderConfigs = [];
        foreach (FolderMonitorConfig folderConfig in configuration.GetSection("Folders").Get<List<FolderMonitorConfig>>() ?? [])
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
            folderConfigs.Add(folderConfig);
        }
        return folderConfigs;
    }
}
