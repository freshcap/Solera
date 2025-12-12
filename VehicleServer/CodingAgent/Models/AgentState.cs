namespace CodingAgent.Models;

public class AgentState
{
    public List<TaskResult> CompletedTasks { get; set; } = new();
    public string? CurrentTaskId { get; set; }
    public DateTime LastCheckpoint { get; set; }
    public int TotalTasksProcessed { get; set; }
    public int SuccessfulTasks { get; set; }
    public int FailedTasks { get; set; }
}
