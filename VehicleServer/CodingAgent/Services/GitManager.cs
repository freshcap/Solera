using LibGit2Sharp;
using Microsoft.Extensions.Logging;

namespace CodingAgent.Services;

public interface IGitManager
{
    Task<bool> CreateCommit(string message, List<string> files);
    Task<bool> CreateBranch(string branchName);
    Task<string> GetCurrentBranch();
    Task<bool> HasUncommittedChanges();
    Task<string?> GetLastCommitHash();
}

public class GitManager : IGitManager
{
    private readonly ILogger<GitManager> _logger;
    private readonly string _repositoryPath;

    public GitManager(ILogger<GitManager> logger, string repositoryPath)
    {
        _logger = logger;
        _repositoryPath = repositoryPath;
    }

    public Task<bool> CreateCommit(string message, List<string> files)
    {
        try
        {
            using var repo = new Repository(_repositoryPath);

            _logger.LogDebug("Creating commit with {FileCount} files", files.Count);

            // Stage the specified files
            foreach (var file in files)
            {
                Commands.Stage(repo, file);
                _logger.LogDebug("Staged file: {FilePath}", file);
            }

            // Create signature
            var signature = repo.Config.BuildSignature(DateTimeOffset.Now);

            // Commit
            var commit = repo.Commit(message, signature, signature);

            _logger.LogInformation("Created commit: {CommitHash} - {Message}",
                commit.Sha.Substring(0, 7), message);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating commit");
            return Task.FromResult(false);
        }
    }

    public Task<bool> CreateBranch(string branchName)
    {
        try
        {
            using var repo = new Repository(_repositoryPath);

            var branch = repo.CreateBranch(branchName);
            Commands.Checkout(repo, branch);

            _logger.LogInformation("Created and checked out branch: {BranchName}", branchName);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branch: {BranchName}", branchName);
            return Task.FromResult(false);
        }
    }

    public Task<string> GetCurrentBranch()
    {
        try
        {
            using var repo = new Repository(_repositoryPath);
            var branchName = repo.Head.FriendlyName;

            _logger.LogDebug("Current branch: {BranchName}", branchName);
            return Task.FromResult(branchName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current branch");
            return Task.FromResult("unknown");
        }
    }

    public Task<bool> HasUncommittedChanges()
    {
        try
        {
            using var repo = new Repository(_repositoryPath);

            var status = repo.RetrieveStatus();
            var hasChanges = status.IsDirty;

            _logger.LogDebug("Repository has uncommitted changes: {HasChanges}", hasChanges);
            return Task.FromResult(hasChanges);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for uncommitted changes");
            return Task.FromResult(false);
        }
    }

    public Task<string?> GetLastCommitHash()
    {
        try
        {
            using var repo = new Repository(_repositoryPath);
            var lastCommit = repo.Head.Tip;

            if (lastCommit == null)
            {
                return Task.FromResult<string?>(null);
            }

            var shortHash = lastCommit.Sha.Substring(0, 7);
            _logger.LogDebug("Last commit hash: {Hash}", shortHash);

            return Task.FromResult<string?>(shortHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting last commit hash");
            return Task.FromResult<string?>(null);
        }
    }
}
