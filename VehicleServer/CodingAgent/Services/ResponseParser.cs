using CodingAgent.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CodingAgent.Services;

public interface IResponseParser
{
    List<FileChange> ParseFileChanges(string claudeResponse);
}

public class ResponseParser : IResponseParser
{
    private readonly ILogger<ResponseParser> _logger;

    public ResponseParser(ILogger<ResponseParser> logger)
    {
        _logger = logger;
    }

    public List<FileChange> ParseFileChanges(string claudeResponse)
    {
        var fileChanges = new List<FileChange>();

        try
        {
            // Split response into sections
            var lines = claudeResponse.Split('\n');
            FileChange? currentChange = null;
            var contentBuilder = new StringBuilder();
            var inContent = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Check for FILE_CHANGE directive
                if (line.StartsWith("FILE_CHANGE:", StringComparison.OrdinalIgnoreCase))
                {
                    // Save previous change if exists
                    if (currentChange != null && inContent)
                    {
                        currentChange.Content = contentBuilder.ToString().TrimEnd();
                        fileChanges.Add(currentChange);
                        contentBuilder.Clear();
                        inContent = false;
                    }

                    var changeTypeStr = line.Substring("FILE_CHANGE:".Length).Trim();
                    currentChange = new FileChange
                    {
                        Type = ParseChangeType(changeTypeStr)
                    };
                }
                // Check for PATH directive
                else if (line.StartsWith("PATH:", StringComparison.OrdinalIgnoreCase) && currentChange != null)
                {
                    currentChange.FilePath = line.Substring("PATH:".Length).Trim();
                }
                // Check for CONTENT_START
                else if (line.Trim() == "CONTENT_START")
                {
                    inContent = true;
                    contentBuilder.Clear();
                }
                // Check for CONTENT_END
                else if (line.Trim() == "CONTENT_END")
                {
                    if (currentChange != null)
                    {
                        currentChange.Content = contentBuilder.ToString().TrimEnd();
                        fileChanges.Add(currentChange);
                        currentChange = null;
                        contentBuilder.Clear();
                        inContent = false;
                    }
                }
                // Collect content
                else if (inContent)
                {
                    contentBuilder.AppendLine(line);
                }
            }

            // Handle case where there's no CONTENT_END (like DELETE operations)
            if (currentChange != null && currentChange.Type == FileChange.ChangeType.Delete)
            {
                fileChanges.Add(currentChange);
            }

            _logger.LogInformation("Parsed {Count} file changes from response", fileChanges.Count);

            foreach (var change in fileChanges)
            {
                _logger.LogDebug("  - {Type}: {Path} ({Length} chars)",
                    change.Type, change.FilePath, change.Content.Length);
            }

            return fileChanges;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing file changes from response");
            return new List<FileChange>();
        }
    }

    private FileChange.ChangeType ParseChangeType(string changeTypeStr)
    {
        return changeTypeStr.ToUpperInvariant() switch
        {
            "CREATE" => FileChange.ChangeType.Create,
            "MODIFY" => FileChange.ChangeType.Modify,
            "DELETE" => FileChange.ChangeType.Delete,
            _ => throw new ArgumentException($"Unknown change type: {changeTypeStr}")
        };
    }
}
