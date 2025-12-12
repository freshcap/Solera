namespace CodingAgent.Models;

public class TaskDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public bool ApprovalRequired { get; set; } = false;
    public string? Phase { get; set; }
    public List<string> ExpectedOutputs { get; set; } = new();
}
