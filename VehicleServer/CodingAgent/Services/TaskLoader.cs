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
            var taskCollection = JsonSerializer.Deserialize<TaskCollection>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (taskCollection?.Tasks == null || taskCollection.Tasks.Count == 0)
            {
                _logger.LogWarning("No tasks found in file: {FilePath}", filePath);
                return new List<TaskDefinition>();
            }

            // Sort by priority
            var sortedTasks = taskCollection.Tasks.OrderBy(t => t.Priority).ToList();

            _logger.LogInformation("Loaded {Count} tasks from {FilePath}", sortedTasks.Count, filePath);

            return sortedTasks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tasks from: {FilePath}", filePath);
            return new List<TaskDefinition>();
        }
    }
}
