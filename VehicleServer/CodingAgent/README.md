# Autonomous Coding Agent

An autonomous coding agent that uses Claude AI to implement features and fix bugs in your .NET + React project.

## Features

- **Task Execution**: Reads tasks from `tasks.json` and executes them autonomously
- **File Management**: Reads source files, applies changes, creates backups
- **Build Integration**: Runs `dotnet build` and `npm run build` after each change
- **Error Recovery**: Automatically retries failed tasks with build errors (up to 3 attempts)
- **Git Integration**: Creates git commits after each successful task
- **Rate Limiting**: Handles API rate limits with exponential backoff
- **State Persistence**: Tracks progress and can resume from interruptions
- **Comprehensive Logging**: Detailed logs of all operations

## Setup

### 1. Configure API Key

Edit `appsettings.json` and add your Anthropic API key:

```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-..."
  }
}
```

### 2. Configure Paths

Update the paths in `appsettings.json` to match your project structure:

```json
{
  "Agent": {
    "WorkingDirectory": "C:\\path\\to\\your\\project",
    "DotNetProjectPath": "VehicleApi/VehicleApi.csproj",
    "NpmProjectPath": "..\\VehicleWebapp",
    "MaxRetries": 3,
    "TasksFilePath": "tasks.json"
  }
}
```

### 3. Define Tasks

Edit `tasks.json` to define the tasks you want the agent to complete:

```json
{
  "tasks": [
    {
      "id": "task-001",
      "description": "Add XML documentation to VehicleController",
      "relevantFiles": [
        "VehicleApi/Controllers/VehicleController.cs"
      ],
      "context": "Add comprehensive XML comments following C# standards",
      "priority": 1
    }
  ]
}
```

## Usage

### Run the Agent

```bash
cd VehicleServer/CodingAgent
dotnet run
```

### Monitor Progress

The agent will:
1. Load all tasks from `tasks.json`
2. For each task:
   - Read the relevant files
   - Call Claude API to generate code changes
   - Apply the changes
   - Run builds (.NET and npm)
   - If builds succeed: create a git commit
   - If builds fail: retry with error feedback (up to 3 times)
3. Save progress to `.agent-state.json`

### Resume After Interruption

The agent automatically tracks completed tasks in `.agent-state.json`. If you stop and restart the agent, it will skip already completed tasks.

## Task Definition Format

Each task in `tasks.json` must have:

- **id**: Unique identifier for the task
- **description**: What needs to be done
- **relevantFiles**: Array of file paths that Claude should read and potentially modify
- **context**: Additional context or requirements
- **priority**: Lower numbers execute first (1, 2, 3, ...)

## Safety Features

1. **Backups**: All modified files are backed up to `.agent-backups/` before changes
2. **Git Commits**: Each successful task creates a separate commit
3. **Build Validation**: Changes are only committed if builds succeed
4. **Retry Logic**: Failed tasks retry up to 3 times with error feedback
5. **State Tracking**: Progress is saved after each task

## File Structure

```
CodingAgent/
├── Models/              # Data structures
│   ├── TaskDefinition.cs
│   ├── TaskResult.cs
│   ├── FileChange.cs
│   ├── AgentState.cs
│   └── BuildResult.cs
├── Services/            # Core services
│   ├── FileSystemManager.cs
│   ├── GitManager.cs
│   ├── BuildRunner.cs
│   ├── PromptBuilder.cs
│   ├── ResponseParser.cs
│   ├── RateLimiter.cs
│   ├── StateManager.cs
│   └── TaskLoader.cs
├── Orchestration/       # Main logic
│   └── AgentOrchestrator.cs
├── Program.cs           # Entry point
├── appsettings.json     # Configuration
├── tasks.json           # Task definitions
└── .agent-state.json    # Progress tracking (auto-generated)
```

## Logging

Logs are output to the console with different levels:
- **Information**: Task progress, build results
- **Debug**: API calls, file operations
- **Warning**: Retries, missing files
- **Error**: Task failures, exceptions

## Rate Limiting

The agent implements rate limiting to respect Anthropic's API limits:
- Max 50 API calls per minute (configurable)
- Exponential backoff on rate limit errors
- Automatic retry with backoff: 1s, 2s, 4s, 8s, 16s, 32s

## Troubleshooting

### Builds Failing
- Check that the paths in `appsettings.json` are correct
- Ensure `dotnet` and `npm` are in your PATH
- Review build errors in the console output

### Tasks Not Completing
- Check the logs for error messages
- Review `.agent-state.json` to see which tasks completed
- Verify that the `relevantFiles` paths are correct

### API Rate Limits
- The agent will automatically handle rate limits
- If you see many rate limit errors, reduce the number of concurrent tasks

## Advanced Configuration

### Adjust Max Retries
```json
"MaxRetries": 5  // Default is 3
```

### Change Working Directory
```json
"WorkingDirectory": "C:\\different\\path"
```

### Use Different Task File
```json
"TasksFilePath": "my-custom-tasks.json"
```

## Example Output

```
========================================
Autonomous Coding Agent Starting
========================================
Working Directory: C:\Coding\Solera\VehicleServer
.NET Project: VehicleApi/VehicleApi.csproj
npm Project: ..\VehicleWebapp
Max Retries: 3
========================================
Loaded 3 tasks
========================================
Starting task: task-001 - Add XML documentation
========================================
Attempt 1/3
Calling Claude API...
Parsed 1 file changes from response
Applying MODIFY: VehicleApi/Controllers/VehicleController.cs
Running builds...
Building .NET project: VehicleApi/VehicleApi.csproj
.NET build succeeded
Building npm project: ..\VehicleWebapp
npm build succeeded
All builds succeeded!
Task completed successfully!
========================================
```
