namespace CodingAgent.Models;

public class FileChange
{
    public enum ChangeType { Create, Modify, Delete }

    public ChangeType Type { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
}
