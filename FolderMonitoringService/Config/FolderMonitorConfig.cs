namespace FolderMonitoringService.Config;

public class FolderMonitorConfig
{
    public required string FolderPath { get; set; }
    public List<string> AllowedExtensions { get; set; } = [];
    public float MaxFileSizeMb { get; set; }
    public bool Enabled { get; set; } = true;
    public bool IncludeSubFolders { get; set; } = false;
    public bool InitialScan { get; set; } = true;
    public float MaxAgeDays { get; set; } = 0;
    public string FolderCheckCron { get; set; } = "";
}