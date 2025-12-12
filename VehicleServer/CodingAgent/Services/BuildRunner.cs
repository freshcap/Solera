using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using CodingAgent.Models;

namespace CodingAgent.Services;

public interface IBuildRunner
{
    Task<BuildResult> BuildDotNet(string projectPath);
    Task<BuildResult> BuildNpm(string projectPath);
}

public class BuildRunner : IBuildRunner
{
    private readonly ILogger<BuildRunner> _logger;
    private readonly string _workingDirectory;

    public BuildRunner(ILogger<BuildRunner> logger, string workingDirectory)
    {
        _logger = logger;
        _workingDirectory = workingDirectory;
    }

    public async Task<BuildResult> BuildDotNet(string projectPath)
    {
        _logger.LogInformation("Starting .NET build for: {ProjectPath}", projectPath);

        var startTime = DateTime.Now;
        var fullPath = Path.Combine(_workingDirectory, projectPath);

        var result = await ExecuteCommand("dotnet", $"build \"{fullPath}\"", _workingDirectory);

        var buildResult = new BuildResult
        {
            Success = result.ExitCode == 0,
            Output = result.Output,
            Duration = DateTime.Now - startTime
        };

        // Parse errors and warnings from dotnet build output
        ParseDotNetOutput(result.Output, buildResult);

        if (buildResult.Success)
        {
            _logger.LogInformation(".NET build succeeded in {Duration}ms", buildResult.Duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogError(".NET build failed with {ErrorCount} errors", buildResult.Errors.Count);
        }

        return buildResult;
    }

    public async Task<BuildResult> BuildNpm(string projectPath)
    {
        _logger.LogInformation("Starting npm build for: {ProjectPath}", projectPath);

        var startTime = DateTime.Now;
        var fullPath = Path.Combine(_workingDirectory, projectPath);

        var result = await ExecuteCommand("npm", "run build", fullPath);

        var buildResult = new BuildResult
        {
            Success = result.ExitCode == 0,
            Output = result.Output,
            Duration = DateTime.Now - startTime
        };

        // Parse errors from npm output
        ParseNpmOutput(result.Output, buildResult);

        if (buildResult.Success)
        {
            _logger.LogInformation("npm build succeeded in {Duration}ms", buildResult.Duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogError("npm build failed with {ErrorCount} errors", buildResult.Errors.Count);
        }

        return buildResult;
    }

    private async Task<(int ExitCode, string Output)> ExecuteCommand(
        string command,
        string arguments,
        string workingDirectory)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                outputBuilder.AppendLine(args.Data);
            }
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                errorBuilder.AppendLine(args.Data);
            }
        };

        _logger.LogDebug("Executing: {Command} {Arguments}", command, arguments);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        var output = outputBuilder.ToString() + errorBuilder.ToString();
        return (process.ExitCode, output);
    }

    private void ParseDotNetOutput(string output, BuildResult buildResult)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Regex to match dotnet build errors: "path/file.cs(line,col): error CS1234: message"
        var errorRegex = new Regex(@"^(.+?)\((\d+),(\d+)\):\s*error\s+(\w+):\s*(.+)$", RegexOptions.Multiline);
        var warningRegex = new Regex(@"^(.+?)\((\d+),(\d+)\):\s*warning\s+(\w+):\s*(.+)$", RegexOptions.Multiline);

        foreach (Match match in errorRegex.Matches(output))
        {
            var errorMessage = $"{match.Groups[1].Value}({match.Groups[2].Value},{match.Groups[3].Value}): " +
                             $"error {match.Groups[4].Value}: {match.Groups[5].Value}";
            buildResult.Errors.Add(errorMessage);
        }

        foreach (Match match in warningRegex.Matches(output))
        {
            var warningMessage = $"{match.Groups[1].Value}({match.Groups[2].Value},{match.Groups[3].Value}): " +
                               $"warning {match.Groups[4].Value}: {match.Groups[5].Value}";
            buildResult.Warnings.Add(warningMessage);
        }

        // Also look for generic error lines
        foreach (var line in lines)
        {
            if (line.Contains("error") && line.Contains(":") && !errorRegex.IsMatch(line))
            {
                buildResult.Errors.Add(line.Trim());
            }
        }
    }

    private void ParseNpmOutput(string output, BuildResult buildResult)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // TypeScript/ESLint errors usually contain "error TS" or just "error"
            if (trimmedLine.Contains("error TS") ||
                trimmedLine.Contains("ERROR in") ||
                (trimmedLine.Contains("error") && trimmedLine.Contains(":")))
            {
                buildResult.Errors.Add(trimmedLine);
            }
            else if (trimmedLine.Contains("warning") && trimmedLine.Contains(":"))
            {
                buildResult.Warnings.Add(trimmedLine);
            }
        }
    }
}
