using Microsoft.Extensions.Logging;
using Common;
using CodingAgent.Models;
using CodingAgent.Services;

namespace CodingAgent.Orchestration;

public class AgentOrchestrator
{
    private readonly IClaudeClient _claudeClient;
    private readonly IFileSystemManager _fileSystem;
    private readonly IGitManager _git;
    private readonly IBuildRunner _buildRunner;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IResponseParser _responseParser;
    private readonly IRateLimiter _rateLimiter;
    private readonly IStateManager _stateManager;
    private readonly IFileDiscoveryService _fileDiscovery;
    private readonly IDiffPreviewService _diffPreview;
    private readonly IApprovalService _approval;
    private readonly ILogger<AgentOrchestrator> _logger;

    private readonly string _dotnetProjectPath;
    private readonly string _npmProjectPath;
    private readonly int _maxRetries;

    public AgentOrchestrator(
        IClaudeClient claudeClient,
        IFileSystemManager fileSystem,
        IGitManager git,
        IBuildRunner buildRunner,
        IPromptBuilder promptBuilder,
        IResponseParser responseParser,
        IRateLimiter rateLimiter,
        IStateManager stateManager,
        IFileDiscoveryService fileDiscovery,
        IDiffPreviewService diffPreview,
        IApprovalService approval,
        ILogger<AgentOrchestrator> logger,
        string dotnetProjectPath,
        string npmProjectPath,
        int maxRetries = 3)
    {
        _claudeClient = claudeClient;
        _fileSystem = fileSystem;
        _git = git;
        _buildRunner = buildRunner;
        _promptBuilder = promptBuilder;
        _responseParser = responseParser;
        _rateLimiter = rateLimiter;
        _stateManager = stateManager;
        _fileDiscovery = fileDiscovery;
        _diffPreview = diffPreview;
        _approval = approval;
        _logger = logger;
        _dotnetProjectPath = dotnetProjectPath;
        _npmProjectPath = npmProjectPath;
        _maxRetries = maxRetries;
    }

    public async Task<bool> ExecuteTask(TaskDefinition task)
    {
        _logger.LogInformation("========================================");
        _logger.LogInformation("Starting task: {TaskId} - {Description}", task.Id, task.Description);
        if (!string.IsNullOrEmpty(task.Phase))
        {
            _logger.LogInformation("Phase: {Phase}", task.Phase);
        }
        _logger.LogInformation("Approval Required: {ApprovalRequired}", task.ApprovalRequired);
        _logger.LogInformation("========================================");

        var taskResult = new TaskResult
        {
            TaskId = task.Id,
            RetryCount = 0
        };

        List<string>? buildErrors = null;
        string? userFeedback = null;

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            _logger.LogInformation("Attempt {Attempt}/{MaxRetries}", attempt, _maxRetries);

            try
            {
                // Step 1: Discover relevant files dynamically
                _logger.LogInformation("Discovering relevant files...");
                var relevantFiles = await _fileDiscovery.DiscoverRelevantFiles(task);

                if (relevantFiles.Count == 0)
                {
                    _logger.LogError("No relevant files discovered for this task");
                    taskResult.Success = false;
                    taskResult.ErrorMessage = "Failed to discover relevant files";
                    await _stateManager.RecordTaskResult(taskResult);
                    return false;
                }

                _logger.LogInformation("Discovered {Count} relevant files", relevantFiles.Count);

                // Step 2: Read relevant files
                var fileContents = await ReadRelevantFiles(relevantFiles);

                if (fileContents.Count == 0)
                {
                    _logger.LogError("No files could be read for this task");
                    taskResult.Success = false;
                    taskResult.ErrorMessage = "Failed to read relevant files";
                    await _stateManager.RecordTaskResult(taskResult);
                    return false;
                }

                // Step 3: Build prompt (with user feedback if retrying)
                var prompt = _promptBuilder.BuildPrompt(task, fileContents, buildErrors);
                if (!string.IsNullOrEmpty(userFeedback))
                {
                    prompt += "\n\n## USER FEEDBACK\n" + userFeedback;
                }

                // Step 4: Call Claude API with rate limiting
                await _rateLimiter.WaitIfNeeded();
                _rateLimiter.RecordApiCall();

                _logger.LogInformation("Calling Claude API...");
                var response = await _claudeClient.GetCompletion(prompt);
                _logger.LogDebug("Received response from Claude ({Length} chars)", response.Length);

                // Step 5: Parse response
                var fileChanges = _responseParser.ParseFileChanges(response);

                if (fileChanges.Count == 0)
                {
                    _logger.LogWarning("No file changes parsed from response");
                    buildErrors = new List<string> { "No file changes were specified in the response" };
                    taskResult.RetryCount++;
                    continue;
                }

                // Step 6: If approval required, show diff and get approval
                if (task.ApprovalRequired)
                {
                    var diffPreview = await _diffPreview.GenerateDiffPreview(fileChanges, _fileSystem);
                    var approvalResult = await _approval.RequestApproval(task, fileChanges, diffPreview);

                    switch (approvalResult.Action)
                    {
                        case ApprovalAction.Reject:
                            _logger.LogWarning("Task rejected by user");
                            taskResult.Success = false;
                            taskResult.ErrorMessage = "Rejected by user";
                            taskResult.CompletedAt = DateTime.Now;
                            await _stateManager.RecordTaskResult(taskResult);
                            return false;

                        case ApprovalAction.Retry:
                            _logger.LogInformation("User requested retry with feedback");
                            userFeedback = approvalResult.Feedback;
                            taskResult.RetryCount++;
                            continue;

                        case ApprovalAction.Abort:
                            _logger.LogWarning("User requested abort");
                            throw new OperationCanceledException("User aborted execution");

                        case ApprovalAction.Approve:
                            _logger.LogInformation("Changes approved by user");
                            break;
                    }
                }

                // Step 7: Apply changes
                var appliedFiles = await ApplyFileChanges(fileChanges);
                taskResult.Changes = fileChanges;

                // Step 8: Run builds
                var buildSuccess = await RunBuilds();

                if (buildSuccess)
                {
                    // Step 9: Commit changes
                    var commitMessage = $"Agent Task {task.Id}: {task.Description}";
                    var committed = await _git.CreateCommit(commitMessage, appliedFiles);

                    if (committed)
                    {
                        var commitHash = await _git.GetLastCommitHash();
                        taskResult.CommitHash = commitHash ?? "unknown";
                    }

                    taskResult.Success = true;
                    taskResult.CompletedAt = DateTime.Now;

                    _logger.LogInformation("Task completed successfully!");
                    await _stateManager.RecordTaskResult(taskResult);
                    return true;
                }
                else
                {
                    // Build failed - collect errors for retry
                    _logger.LogWarning("Build failed. Collecting errors for retry...");
                    buildErrors = await CollectBuildErrors();
                    taskResult.BuildErrors = buildErrors;
                    taskResult.RetryCount++;

                    if (attempt < _maxRetries)
                    {
                        _logger.LogInformation("Retrying with build errors...");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // User aborted
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during task execution attempt {Attempt}", attempt);
                taskResult.ErrorMessage = ex.Message;
                taskResult.RetryCount++;

                if (attempt < _maxRetries)
                {
                    buildErrors = new List<string> { $"Error: {ex.Message}" };
                }
            }
        }

        // All retries exhausted
        _logger.LogError("Task failed after {MaxRetries} attempts", _maxRetries);
        taskResult.Success = false;
        taskResult.CompletedAt = DateTime.Now;
        await _stateManager.RecordTaskResult(taskResult);

        return false;
    }

    private async Task<Dictionary<string, string>> ReadRelevantFiles(List<string> filePaths)
    {
        var fileContents = new Dictionary<string, string>();

        foreach (var filePath in filePaths)
        {
            try
            {
                var exists = await _fileSystem.FileExists(filePath);

                if (exists)
                {
                    var content = await _fileSystem.ReadFile(filePath);
                    fileContents[filePath] = content;
                    _logger.LogDebug("Read file: {FilePath} ({Length} chars)", filePath, content.Length);
                }
                else
                {
                    _logger.LogWarning("File not found: {FilePath} (will be created if needed)", filePath);
                    fileContents[filePath] = "// File does not exist yet";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
            }
        }

        return fileContents;
    }

    private async Task<List<string>> ApplyFileChanges(List<FileChange> fileChanges)
    {
        var appliedFiles = new List<string>();

        foreach (var change in fileChanges)
        {
            try
            {
                _logger.LogInformation("Applying {ChangeType}: {FilePath}", change.Type, change.FilePath);

                switch (change.Type)
                {
                    case FileChange.ChangeType.Create:
                    case FileChange.ChangeType.Modify:
                        // Create backup if file exists
                        if (await _fileSystem.FileExists(change.FilePath))
                        {
                            await _fileSystem.CreateBackup(change.FilePath);
                        }

                        await _fileSystem.WriteFile(change.FilePath, change.Content);
                        appliedFiles.Add(change.FilePath);
                        break;

                    case FileChange.ChangeType.Delete:
                        await _fileSystem.CreateBackup(change.FilePath);
                        await _fileSystem.DeleteFile(change.FilePath);
                        appliedFiles.Add(change.FilePath);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying change to: {FilePath}", change.FilePath);
            }
        }

        return appliedFiles;
    }

    private async Task<bool> RunBuilds()
    {
        _logger.LogInformation("Running builds...");

        // Build .NET project
        _logger.LogInformation("Building .NET project: {Path}", _dotnetProjectPath);
        var dotnetResult = await _buildRunner.BuildDotNet(_dotnetProjectPath);

        if (!dotnetResult.Success)
        {
            _logger.LogError(".NET build failed with {ErrorCount} errors", dotnetResult.Errors.Count);
            return false;
        }

        _logger.LogInformation(".NET build succeeded");

        // Build npm project
        _logger.LogInformation("Building npm project: {Path}", _npmProjectPath);
        var npmResult = await _buildRunner.BuildNpm(_npmProjectPath);

        if (!npmResult.Success)
        {
            _logger.LogError("npm build failed with {ErrorCount} errors", npmResult.Errors.Count);
            return false;
        }

        _logger.LogInformation("npm build succeeded");
        _logger.LogInformation("All builds succeeded!");

        return true;
    }

    private async Task<List<string>> CollectBuildErrors()
    {
        var allErrors = new List<string>();

        try
        {
            var dotnetResult = await _buildRunner.BuildDotNet(_dotnetProjectPath);
            allErrors.AddRange(dotnetResult.Errors);

            var npmResult = await _buildRunner.BuildNpm(_npmProjectPath);
            allErrors.AddRange(npmResult.Errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting build errors");
            allErrors.Add($"Error collecting build errors: {ex.Message}");
        }

        return allErrors;
    }
}
