using System.Text;
using Microsoft.Extensions.Logging;
using CodingAgent.Models;

namespace CodingAgent.Services;

public interface IPromptBuilder
{
    string BuildPrompt(TaskDefinition task, Dictionary<string, string> fileContents, List<string>? buildErrors = null);
}

public class PromptBuilder : IPromptBuilder
{
    private readonly ILogger<PromptBuilder> _logger;

    public PromptBuilder(ILogger<PromptBuilder> logger)
    {
        _logger = logger;
    }

    public string BuildPrompt(TaskDefinition task, Dictionary<string, string> fileContents, List<string>? buildErrors = null)
    {
        var prompt = new StringBuilder();

        prompt.AppendLine("You are an autonomous coding assistant. Your task is to make precise code changes to implement the following requirement.");
        prompt.AppendLine();

        prompt.AppendLine("## TASK");
        prompt.AppendLine($"Task ID: {task.Id}");
        prompt.AppendLine($"Description: {task.Description}");

        if (!string.IsNullOrEmpty(task.Context))
        {
            prompt.AppendLine($"Context: {task.Context}");
        }

        prompt.AppendLine();

        // Add relevant files
        prompt.AppendLine("## CURRENT CODE");
        prompt.AppendLine("Below are the relevant files for this task:");
        prompt.AppendLine();

        foreach (var (filePath, content) in fileContents)
        {
            prompt.AppendLine($"### File: {filePath}");
            prompt.AppendLine("```");
            prompt.AppendLine(content);
            prompt.AppendLine("```");
            prompt.AppendLine();
        }

        // If there are build errors, include them
        if (buildErrors != null && buildErrors.Count > 0)
        {
            prompt.AppendLine("## BUILD ERRORS");
            prompt.AppendLine("The previous attempt resulted in the following build errors that need to be fixed:");
            prompt.AppendLine();

            foreach (var error in buildErrors)
            {
                prompt.AppendLine($"- {error}");
            }

            prompt.AppendLine();
        }

        // Instructions for response format
        prompt.AppendLine("## INSTRUCTIONS");
        prompt.AppendLine("Provide your response in the following format:");
        prompt.AppendLine();
        prompt.AppendLine("1. Brief explanation of changes (2-3 sentences)");
        prompt.AppendLine("2. For each file that needs to be modified or created, use this exact format:");
        prompt.AppendLine();
        prompt.AppendLine("FILE_CHANGE: <CREATE|MODIFY|DELETE>");
        prompt.AppendLine("PATH: <relative/path/to/file>");
        prompt.AppendLine("CONTENT_START");
        prompt.AppendLine("<complete file content here>");
        prompt.AppendLine("CONTENT_END");
        prompt.AppendLine();
        prompt.AppendLine("IMPORTANT:");
        prompt.AppendLine("- Always provide the COMPLETE file content, not just the changes");
        prompt.AppendLine("- Use exact spacing and indentation");
        prompt.AppendLine("- For MODIFY operations, include the entire updated file");
        prompt.AppendLine("- For DELETE operations, omit the CONTENT_START/CONTENT_END blocks");
        prompt.AppendLine("- Make minimal changes - only what's necessary to complete the task");
        prompt.AppendLine("- Ensure code compiles and follows existing patterns");

        var finalPrompt = prompt.ToString();
        _logger.LogDebug("Built prompt with {FileCount} files, length: {Length} characters",
            fileContents.Count, finalPrompt.Length);

        return finalPrompt;
    }
}
