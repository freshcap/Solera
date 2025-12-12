using Microsoft.Extensions.Logging;
using CodingAgent.Models;

namespace CodingAgent.Services;

public interface IFileSystemManager
{
    Task<string> ReadFile(string path);
    Task WriteFile(string path, string content);
    Task<bool> FileExists(string path);
    Task CreateBackup(string path);
    Task<List<string>> FindFiles(string pattern, string searchPath);
    Task DeleteFile(string path);
    string GetFullPath(string relativePath);
}

public class FileSystemManager : IFileSystemManager
{
    private readonly ILogger<FileSystemManager> _logger;
    private readonly string _workingDirectory;
    private readonly string _backupDirectory;

    public FileSystemManager(ILogger<FileSystemManager> logger, string workingDirectory)
    {
        _logger = logger;
        _workingDirectory = workingDirectory;
        _backupDirectory = Path.Combine(_workingDirectory, ".agent-backups");

        if (!Directory.Exists(_backupDirectory))
        {
            Directory.CreateDirectory(_backupDirectory);
        }
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(_workingDirectory, relativePath);
    }

    public async Task<string> ReadFile(string path)
    {
        try
        {
            var fullPath = GetFullPath(path);
            _logger.LogDebug("Reading file: {FilePath}", fullPath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {fullPath}");
            }

            return await File.ReadAllTextAsync(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading file: {FilePath}", path);
            throw;
        }
    }

    public async Task WriteFile(string path, string content)
    {
        try
        {
            var fullPath = GetFullPath(path);
            _logger.LogDebug("Writing file: {FilePath}", fullPath);

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content);
            _logger.LogInformation("Successfully wrote file: {FilePath}", path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing file: {FilePath}", path);
            throw;
        }
    }

    public Task<bool> FileExists(string path)
    {
        var fullPath = GetFullPath(path);
        return Task.FromResult(File.Exists(fullPath));
    }

    public async Task CreateBackup(string path)
    {
        try
        {
            var fullPath = GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Cannot backup non-existent file: {FilePath}", path);
                return;
            }

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var fileName = Path.GetFileName(path);
            var backupFileName = $"{fileName}.{timestamp}.bak";
            var backupPath = Path.Combine(_backupDirectory, backupFileName);

            await File.WriteAllTextAsync(backupPath, await File.ReadAllTextAsync(fullPath));
            _logger.LogInformation("Created backup: {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup for: {FilePath}", path);
            throw;
        }
    }

    public Task<List<string>> FindFiles(string pattern, string searchPath)
    {
        try
        {
            var fullSearchPath = GetFullPath(searchPath);
            _logger.LogDebug("Finding files matching pattern: {Pattern} in {Path}", pattern, fullSearchPath);

            if (!Directory.Exists(fullSearchPath))
            {
                _logger.LogWarning("Search path does not exist: {Path}", fullSearchPath);
                return Task.FromResult(new List<string>());
            }

            var files = Directory.GetFiles(fullSearchPath, pattern, SearchOption.AllDirectories)
                .Select(f => Path.GetRelativePath(_workingDirectory, f))
                .ToList();

            _logger.LogDebug("Found {Count} files matching pattern", files.Count);
            return Task.FromResult(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding files with pattern: {Pattern}", pattern);
            throw;
        }
    }

    public async Task DeleteFile(string path)
    {
        try
        {
            var fullPath = GetFullPath(path);
            _logger.LogDebug("Deleting file: {FilePath}", fullPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Successfully deleted file: {FilePath}", path);
            }
            else
            {
                _logger.LogWarning("Cannot delete non-existent file: {FilePath}", path);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", path);
            throw;
        }
    }
}
