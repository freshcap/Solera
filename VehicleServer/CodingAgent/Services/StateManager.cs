using System.Text.Json;
using Microsoft.Extensions.Logging;
using CodingAgent.Models;

namespace CodingAgent.Services;

public interface IStateManager
{
    Task<AgentState> LoadState();
    Task SaveState(AgentState state);
    Task RecordTaskResult(TaskResult result);
    Task<AgentState> GetCurrentState();
}

public class StateManager : IStateManager
{
    private readonly ILogger<StateManager> _logger;
    private readonly string _stateFilePath;
    private AgentState _currentState;

    public StateManager(ILogger<StateManager> logger, string workingDirectory)
    {
        _logger = logger;
        _stateFilePath = Path.Combine(workingDirectory, ".agent-state.json");
        _currentState = new AgentState
        {
            LastCheckpoint = DateTime.Now
        };
    }

    public async Task<AgentState> LoadState()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = await File.ReadAllTextAsync(_stateFilePath);
                var state = JsonSerializer.Deserialize<AgentState>(json);

                if (state != null)
                {
                    _currentState = state;
                    _logger.LogInformation(
                        "Loaded state: {Total} tasks processed ({Success} successful, {Failed} failed)",
                        state.TotalTasksProcessed, state.SuccessfulTasks, state.FailedTasks);

                    return state;
                }
            }

            _logger.LogInformation("No existing state found. Starting fresh.");
            return _currentState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading state. Starting fresh.");
            return new AgentState { LastCheckpoint = DateTime.Now };
        }
    }

    public async Task SaveState(AgentState state)
    {
        try
        {
            state.LastCheckpoint = DateTime.Now;
            _currentState = state;

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(state, options);
            await File.WriteAllTextAsync(_stateFilePath, json);

            _logger.LogDebug("State saved: {Total} tasks processed", state.TotalTasksProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving state");
        }
    }

    public async Task RecordTaskResult(TaskResult result)
    {
        try
        {
            _currentState.CompletedTasks.Add(result);
            _currentState.TotalTasksProcessed++;

            if (result.Success)
            {
                _currentState.SuccessfulTasks++;
            }
            else
            {
                _currentState.FailedTasks++;
            }

            _currentState.CurrentTaskId = null;

            await SaveState(_currentState);

            _logger.LogInformation(
                "Task {TaskId} recorded. Status: {Status}. Total: {Total} ({Success} successful)",
                result.TaskId, result.Success ? "SUCCESS" : "FAILED",
                _currentState.TotalTasksProcessed, _currentState.SuccessfulTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording task result");
        }
    }

    public Task<AgentState> GetCurrentState()
    {
        return Task.FromResult(_currentState);
    }
}
