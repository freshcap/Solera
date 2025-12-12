using Microsoft.Extensions.Logging;
using CodingAgent.Models;

namespace CodingAgent.Services;

public interface IApprovalService
{
    Task<ApprovalResult> RequestApproval(TaskDefinition task, List<FileChange> proposedChanges, string diffPreview);
}

public class ApprovalResult
{
    public bool Approved { get; set; }
    public string? Feedback { get; set; }
    public ApprovalAction Action { get; set; }
}

public enum ApprovalAction
{
    Approve,      // Continue with the changes
    Reject,       // Skip this task
    Retry,        // Retry with feedback
    Abort         // Stop the entire agent
}

public class ApprovalService : IApprovalService
{
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(ILogger<ApprovalService> logger)
    {
        _logger = logger;
    }

    public async Task<ApprovalResult> RequestApproval(
        TaskDefinition task,
        List<FileChange> proposedChanges,
        string diffPreview)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("APPROVAL REQUIRED");
        _logger.LogInformation("========================================");
        _logger.LogInformation("Task: {TaskId} - {Description}", task.Id, task.Description);
        _logger.LogInformation("Proposed changes: {Count} files", proposedChanges.Count);
        _logger.LogInformation("");

        // Display diff preview
        Console.WriteLine(diffPreview);

        // Prompt for approval
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("Review the proposed changes above.");
        Console.WriteLine("========================================");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  [A] Approve - Apply these changes");
        Console.WriteLine("  [R] Reject  - Skip this task");
        Console.WriteLine("  [F] Feedback - Retry with feedback");
        Console.WriteLine("  [Q] Quit    - Stop the agent");
        Console.WriteLine();
        Console.Write("Your choice [A/R/F/Q]: ");

        var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

        switch (choice)
        {
            case "A":
            case "APPROVE":
                _logger.LogInformation("Changes approved by user");
                return new ApprovalResult
                {
                    Approved = true,
                    Action = ApprovalAction.Approve
                };

            case "R":
            case "REJECT":
                _logger.LogWarning("Changes rejected by user");
                return new ApprovalResult
                {
                    Approved = false,
                    Action = ApprovalAction.Reject
                };

            case "F":
            case "FEEDBACK":
                Console.WriteLine();
                Console.Write("Enter your feedback: ");
                var feedback = Console.ReadLine();

                _logger.LogInformation("User provided feedback for retry: {Feedback}", feedback);
                return new ApprovalResult
                {
                    Approved = false,
                    Action = ApprovalAction.Retry,
                    Feedback = feedback
                };

            case "Q":
            case "QUIT":
                _logger.LogWarning("User requested to abort agent");
                return new ApprovalResult
                {
                    Approved = false,
                    Action = ApprovalAction.Abort
                };

            default:
                _logger.LogWarning("Invalid choice: {Choice}. Treating as rejection.", choice);
                return new ApprovalResult
                {
                    Approved = false,
                    Action = ApprovalAction.Reject
                };
        }
    }
}
