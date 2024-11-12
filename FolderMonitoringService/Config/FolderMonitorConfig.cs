namespace FolderMonitoringService.Config;

public class FolderMonitorConfig
{
    public required string FolderPath { get; set; }
    public List<string> AllowedExtensions { get; set; } = [];
    public float MaxFileSizeMb { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsIncludeSubFolders { get; set; } = false;
}