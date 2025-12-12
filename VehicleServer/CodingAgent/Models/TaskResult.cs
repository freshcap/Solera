namespace CodingAgent.Models;

public class TaskResult
{
    public string TaskId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public List<FileChange> Changes { get; set; } = new();
    public List<string> BuildErrors { get; set; } = new();
    public int RetryCount { get; set; }
    public DateTime CompletedAt { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string CommitHash { get; set; } = string.Empty;
}
