using Microsoft.Extensions.Logging;
using CodingAgent.Models;

namespace CodingAgent.Services;

public interface IDiffPreviewService
{
    Task<string> GenerateDiffPreview(List<FileChange> fileChanges, IFileSystemManager fileSystem);
}

public class DiffPreviewService : IDiffPreviewService
{
    private readonly ILogger<DiffPreviewService> _logger;

    public DiffPreviewService(ILogger<DiffPreviewService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateDiffPreview(List<FileChange> fileChanges, IFileSystemManager fileSystem)
    {
        var preview = new System.Text.StringBuilder();

        preview.AppendLine("========================================");
        preview.AppendLine("PROPOSED CHANGES");
        preview.AppendLine("========================================");
        preview.AppendLine();

        foreach (var change in fileChanges)
        {
            preview.AppendLine($"File: {change.FilePath}");
            preview.AppendLine($"Action: {change.Type}");
            preview.AppendLine("----------------------------------------");

            switch (change.Type)
            {
                case FileChange.ChangeType.Create:
                    preview.AppendLine("[NEW FILE]");
                    preview.AppendLine();
                    preview.AppendLine(GetPreview(change.Content));
                    break;

                case FileChange.ChangeType.Modify:
                    var originalContent = await GetOriginalContent(change.FilePath, fileSystem);
                    preview.AppendLine(GenerateUnifiedDiff(originalContent, change.Content, change.FilePath));
                    break;

                case FileChange.ChangeType.Delete:
                    preview.AppendLine("[FILE WILL BE DELETED]");
                    break;
            }

            preview.AppendLine();
            preview.AppendLine("========================================");
            preview.AppendLine();
        }

        return preview.ToString();
    }

    private async Task<string> GetOriginalContent(string filePath, IFileSystemManager fileSystem)
    {
        try
        {
            if (await fileSystem.FileExists(filePath))
            {
                return await fileSystem.ReadFile(filePath);
            }
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read original file: {FilePath}", filePath);
            return string.Empty;
        }
    }

    private string GenerateUnifiedDiff(string original, string modified, string filePath)
    {
        var diff = new System.Text.StringBuilder();

        var originalLines = original.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        var modifiedLines = modified.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

        // Simple diff - show side by side comparison for small files
        if (originalLines.Length < 100 && modifiedLines.Length < 100)
        {
            diff.AppendLine("[FULL FILE COMPARISON]");
            diff.AppendLine();

            // Show line-by-line differences
            var maxLines = Math.Max(originalLines.Length, modifiedLines.Length);
            var changedLines = 0;

            for (int i = 0; i < maxLines && changedLines < 50; i++)
            {
                var origLine = i < originalLines.Length ? originalLines[i] : null;
                var modLine = i < modifiedLines.Length ? modifiedLines[i] : null;

                if (origLine != modLine)
                {
                    if (origLine != null)
                    {
                        diff.AppendLine($"- {origLine}");
                    }
                    if (modLine != null)
                    {
                        diff.AppendLine($"+ {modLine}");
                    }
                    changedLines++;
                }
            }

            if (changedLines >= 50)
            {
                diff.AppendLine("... (diff truncated, too many changes)");
            }
        }
        else
        {
            // For larger files, show summary
            diff.AppendLine($"[FILE MODIFIED: {originalLines.Length} lines -> {modifiedLines.Length} lines]");
            diff.AppendLine();
            diff.AppendLine("First 20 lines of new content:");
            diff.AppendLine(GetPreview(modified, 20));
        }

        return diff.ToString();
    }

    private string GetPreview(string content, int maxLines = 30)
    {
        var lines = content.Split('\n').Take(maxLines).ToArray();
        var preview = string.Join('\n', lines);

        if (content.Split('\n').Length > maxLines)
        {
            preview += "\n... (truncated)";
        }

        return preview;
    }
}
