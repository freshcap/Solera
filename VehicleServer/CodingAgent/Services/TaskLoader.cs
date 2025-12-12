using System.Text.Json;
using Microsoft.Extensions.Logging;
using CodingAgent.Models;

namespace CodingAgent.Services;

public interface ITaskLoader
{
    Task<List<TaskDefinition>> LoadTasks(string filePath);
}

public class TaskLoader : ITaskLoader
{
    private readonly ILogger<TaskLoader> _logger;

    public TaskLoader(ILogger<TaskLoader> logger)
    {
        _logger = logger;
    }

    public async Task<List<TaskDefinition>> LoadTasks(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("Task file not found: {FilePath}", filePath);
                return new List<TaskDefinition>();
            }

            var json = await File.ReadAllTextAsync(filePath);
            var tasks = JsonSerializer.Deserialize<List<TaskDefinition>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];

            if (tasks.Count == 0)
            {
                _logger.LogWarning("No tasks found in file: {FilePath}", filePath);
                return new List<TaskDefinition>();
            }

            _logger.LogInformation("Loaded {Count} tasks from {FilePath}", tasks.Count, filePath);

            return tasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tasks from: {FilePath}", filePath);
            return new List<TaskDefinition>();
        }
    }
}
