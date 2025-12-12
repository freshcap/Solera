using Microsoft.Extensions.Logging;
using Common;
using CodingAgent.Models;

namespace CodingAgent.Services;

public interface IFileDiscoveryService
{
    Task<List<string>> DiscoverRelevantFiles(TaskDefinition task);
}

public class FileDiscoveryService : IFileDiscoveryService
{
    private readonly IClaudeClient _claudeClient;
    private readonly IFileSystemManager _fileSystem;
    private readonly ILogger<FileDiscoveryService> _logger;
    private readonly string _workingDirectory;

    public FileDiscoveryService(
        IClaudeClient claudeClient,
        IFileSystemManager fileSystem,
        ILogger<FileDiscoveryService> logger,
        string workingDirectory)
    {
        _claudeClient = claudeClient;
        _fileSystem = fileSystem;
        _logger = logger;
        _workingDirectory = workingDirectory;
    }

    public async Task<List<string>> DiscoverRelevantFiles(TaskDefinition task)
    {
        _logger.LogInformation("Discovering relevant files for task: {TaskId}", task.Id);

        try
        {
            // Get project structure
            var projectStructure = await GetProjectStructure();

            // Build discovery prompt
            var prompt = BuildDiscoveryPrompt(task, projectStructure);

            // Call Claude to analyze
            var response = await _claudeClient.GetCompletion(prompt);

            // Parse file paths from response
            var files = ParseFilePaths(response);

            _logger.LogInformation("Discovered {Count} relevant files", files.Count);
            foreach (var file in files)
            {
                _logger.LogDebug("  - {FilePath}", file);
            }

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering relevant files");
            return new List<string>();
        }
    }

    private async Task<string> GetProjectStructure()
    {
        var structure = new System.Text.StringBuilder();

        structure.AppendLine("Project Structure:");
        structure.AppendLine();

        // Scan for .cs files (limit to important directories)
        var csFiles = await _fileSystem.FindFiles("*.cs", ".");
        var filteredCs = csFiles
            .Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\") && !f.Contains("\\CodingAgent\\"))
            .Take(100)
            .ToList();

        structure.AppendLine("C# Files:");
        foreach (var file in filteredCs)
        {
            structure.AppendLine($"  - {file}");
        }

        // Scan for .tsx and .ts files
        try
        {
            var parentDir = Directory.GetParent(_workingDirectory)?.FullName;
            if (parentDir != null)
            {
                var webappPath = Path.Combine(parentDir, "VehicleWebapp");
                if (Directory.Exists(webappPath))
                {
                    var tsFiles = Directory.GetFiles(webappPath, "*.tsx", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles(webappPath, "*.ts", SearchOption.AllDirectories))
                        .Where(f => !f.Contains("node_modules"))
                        .Select(f => Path.GetRelativePath(_workingDirectory, f))
                        .Take(50)
                        .ToList();

                    structure.AppendLine();
                    structure.AppendLine("TypeScript/React Files:");
                    foreach (var file in tsFiles)
                    {
                        structure.AppendLine($"  - {file}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not scan webapp directory");
        }

        return structure.ToString();
    }

    private string BuildDiscoveryPrompt(TaskDefinition task, string projectStructure)
    {
        var prompt = new System.Text.StringBuilder();

        prompt.AppendLine("You are analyzing a coding task to determine which files are relevant.");
        prompt.AppendLine();
        prompt.AppendLine("## TASK");
        prompt.AppendLine($"Description: {task.Description}");
        prompt.AppendLine($"Context: {task.Context}");

        if (!string.IsNullOrEmpty(task.Phase))
        {
            prompt.AppendLine($"Phase: {task.Phase}");
        }

        if (task.ExpectedOutputs.Any())
        {
            prompt.AppendLine("Expected outputs:");
            foreach (var output in task.ExpectedOutputs)
            {
                prompt.AppendLine($"  - {output}");
            }
        }

        prompt.AppendLine();
        prompt.AppendLine("## PROJECT STRUCTURE");
        prompt.AppendLine(projectStructure);
        prompt.AppendLine();

        prompt.AppendLine("## INSTRUCTIONS");
        prompt.AppendLine("Based on the task description and project structure, identify which files are relevant.");
        prompt.AppendLine("Consider:");
        prompt.AppendLine("1. Files that need to be read to understand the context");
        prompt.AppendLine("2. Files that will need to be modified");
        prompt.AppendLine("3. Files that need to be created (if mentioned in expected outputs)");
        prompt.AppendLine("4. Related files (e.g., if modifying a controller, include its models/services)");
        prompt.AppendLine();
        prompt.AppendLine("Respond with ONLY a list of file paths, one per line, no explanations.");
        prompt.AppendLine("Use relative paths from the project root.");
        prompt.AppendLine("Example format:");
        prompt.AppendLine("VehicleApi/Controllers/VehicleController.cs");
        prompt.AppendLine("VehicleApi/Models/Vehicle.cs");
        prompt.AppendLine("VehicleApi/Services/VehicleService.cs");

        return prompt.ToString();
    }

    private List<string> ParseFilePaths(string response)
    {
        var files = new List<string>();

        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip lines that look like explanations or headers
            if (trimmed.StartsWith("#") ||
                trimmed.StartsWith("//") ||
                trimmed.StartsWith("*") ||
                trimmed.Contains(":") ||
                trimmed.Length < 3)
            {
                continue;
            }

            // Remove common prefixes
            trimmed = trimmed.TrimStart('-', ' ', '\t');

            // Check if it looks like a file path
            if (trimmed.Contains("/") || trimmed.Contains("\\"))
            {
                // Normalize path separators
                trimmed = trimmed.Replace("\\", "/");
                files.Add(trimmed);
            }
        }

        return files.Distinct().ToList();
    }
}
