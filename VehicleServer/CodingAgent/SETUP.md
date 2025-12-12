# CodingAgent Setup Guide

This guide will help you set up and run the Autonomous Coding Agent.

## Prerequisites

- .NET 10 SDK installed
- Node.js and npm installed (for webapp builds)
- Git repository initialized
- Anthropic API key

## Quick Start

### Step 1: Configure API Key

1. Open `appsettings.json`
2. Replace `YOUR_API_KEY_HERE` with your actual Anthropic API key:

```json
{
  "Anthropic": {
    "ApiKey": "sk-ant-api03-xxxxx"
  }
}
```

**IMPORTANT**: Add `appsettings.json` to your `.gitignore` to avoid committing your API key!

### Step 2: Verify Paths

The default paths in `appsettings.json` are configured for the Solera project structure:

```json
{
  "Agent": {
    "WorkingDirectory": "C:\\Coding\\Solera\\VehicleServer",
    "DotNetProjectPath": "VehicleApi/VehicleApi.csproj",
    "NpmProjectPath": "..\\VehicleWebapp"
  }
}
```

**Update these paths if your project structure is different!**

### Step 3: Define Your Tasks

Edit `tasks.json` to define the coding tasks you want the agent to complete.

Example task:

```json
{
  "id": "task-001",
  "description": "Add logging to the GetVehicles endpoint",
  "context": "Add ILogger dependency injection and log entry/exit of GetVehicles method",
  "approvalRequired": false,
  "phase": "api",
  "expectedOutputs": []
}
```

**Autonomy fields:**
- `approvalRequired`: Set to `true` to review changes before applying
- `phase`: Organize tasks by category (e.g., "api", "database", "react")
- `expectedOutputs`: Hint which files should be created/modified

**Note:**
- Tasks execute in the order they appear in the JSON array
- The agent automatically discovers relevant files based on task description

### Step 4: Run the Agent

**Normal mode (applies changes):**
```cmd
cd VehicleServer\CodingAgent
dotnet run
```

**Dry-run mode (preview only):**
```cmd
cd VehicleServer\CodingAgent
dotnet run -- --dry-run
```

Use `--dry-run` to preview changes without applying them. This is useful for:
- Testing new task definitions
- Reviewing what the agent will do before committing
- Experimenting with different prompts safely

## Task Definition Best Practices

### 1. Be Specific in Description
❌ Bad: "Improve the controller"
✅ Good: "Add input validation to all VehicleController endpoints"

### 2. Use Expected Outputs (Optional)
If you know which files should be created/modified, list them as hints:

```json
"expectedOutputs": [
  "VehicleApi/Controllers/HealthController.cs"
]
```

The agent will automatically discover other relevant files based on your task description.

### 3. Provide Context
Give Claude enough context to understand the requirements:

```json
"context": "Follow the existing validation pattern used in UserController. Return 400 Bad Request for invalid inputs with descriptive error messages."
```

### 4. Order Tasks
Tasks are executed in the order they appear in the array. Arrange your tasks from first to last:

```json
{
  "tasks": [
    { "id": "task-001", ... },  // Runs first
    { "id": "task-002", ... },  // Runs second
    { "id": "task-003", ... }   // Runs third
  ]
}
```

## Configuration Options

### Agent Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `WorkingDirectory` | Current directory | Root directory of your .NET project |
| `DotNetProjectPath` | `VehicleApi/VehicleApi.csproj` | Relative path to .csproj file |
| `NpmProjectPath` | `..\\VehicleWebapp` | Relative path to npm project folder |
| `MaxRetries` | `3` | Number of retry attempts per task |
| `TasksFilePath` | `tasks.json` | Path to task definitions file |

### Logging Levels

Edit `appsettings.json` to adjust logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",      // General logs
      "CodingAgent": "Debug"         // Detailed agent logs
    }
  }
}
```

Available levels: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`

## Understanding Agent Behavior

### Execution Flow

For each task, the agent:

1. **Reads** all files listed in `relevantFiles`
2. **Builds prompt** with task description + file contents
3. **Calls Claude API** to get code changes
4. **Parses response** to extract file modifications
5. **Creates backups** of files being modified
6. **Applies changes** to the files
7. **Runs builds**:
   - `dotnet build` on the .NET project
   - `npm run build` on the webapp
8. **If builds succeed**:
   - Creates git commit with message: `Agent Task {id}: {description}`
   - Marks task as successful
   - Moves to next task
9. **If builds fail**:
   - Collects build errors
   - Retries with errors included in prompt (up to `MaxRetries`)
   - If all retries fail, marks task as failed and moves on

### State Management

The agent tracks progress in `.agent-state.json`:

```json
{
  "CompletedTasks": [...],
  "TotalTasksProcessed": 5,
  "SuccessfulTasks": 4,
  "FailedTasks": 1,
  "LastCheckpoint": "2024-11-25T10:30:00"
}
```

**Benefits**:
- Can resume if interrupted
- Skips already-completed tasks
- Tracks success/failure statistics

### Safety Features

1. **Backups**: All modified files are backed up to `.agent-backups/`
2. **Git Commits**: Each successful task gets its own commit
3. **Build Validation**: Changes only committed if builds pass
4. **Retry Logic**: Failed tasks retry with error feedback
5. **Rate Limiting**: Respects API limits with exponential backoff

## Troubleshooting

### Issue: "Anthropic API key not configured"

**Solution**: Add your API key to `appsettings.json`:
```json
"Anthropic": {
  "ApiKey": "sk-ant-api03-xxxxx"
}
```

### Issue: Build errors for unrelated files

**Problem**: The builds include files not being modified by the agent.

**Solution**: Fix existing build errors manually first, or use a clean branch.

### Issue: Tasks completing but making wrong changes

**Problem**: Task description or context is too vague.

**Solution**: Make task descriptions more specific:
- Include exact requirements
- Reference existing patterns to follow
- Provide examples of expected output

### Issue: API rate limit errors

**Problem**: Too many API calls in a short time.

**Solution**: The agent automatically handles rate limits. If you see persistent issues:
- Reduce the number of tasks
- Increase delays between tasks
- Check your API tier limits

### Issue: Git commit failures

**Problem**: Git user not configured or repository issues.

**Solution**: Configure git:
```cmd
git config user.name "Your Name"
git config user.email "your.email@example.com"
```

## Advanced Usage

### Custom Claude Model

Edit `Common/ClaudeClientBase.cs` to change the model:

```csharp
var request = new
{
    model = "claude-sonnet-4-20250514",  // Change this
    max_tokens = 2000,
    // ...
};
```

### Increase Max Tokens

For larger code changes, increase max_tokens:

```csharp
max_tokens = 4000,  // Default is 2000
```

### Running Specific Tasks

Temporarily remove tasks from `tasks.json` to run only specific ones, or reorder them in the array.

## File Locations

- **Backups**: `.agent-backups/`
- **State**: `.agent-state.json`
- **Logs**: Console output (redirect to file if needed: `dotnet run > agent.log 2>&1`)

## Example Session

```cmd
C:\Coding\Solera\VehicleServer\CodingAgent> dotnet run

========================================
Autonomous Coding Agent Starting
========================================
Working Directory: C:\Coding\Solera\VehicleServer
.NET Project: VehicleApi/VehicleApi.csproj
npm Project: ..\VehicleWebapp
Max Retries: 3
========================================
Loaded 2 tasks

========================================
Starting task: task-001 - Add XML documentation
========================================
Attempt 1/3
Reading file: VehicleApi/Controllers/VehicleController.cs (2543 chars)
Calling Claude API...
Received response from Claude (3821 chars)
Parsed 1 file changes from response
  - MODIFY: VehicleApi/Controllers/VehicleController.cs (2847 chars)
Applying MODIFY: VehicleApi/Controllers/VehicleController.cs
Created backup: .agent-backups/VehicleController.cs.20241125-103045.bak
Successfully wrote file: VehicleApi/Controllers/VehicleController.cs
Running builds...
Building .NET project: VehicleApi/VehicleApi.csproj
.NET build succeeded in 2134ms
Building npm project: ..\VehicleWebapp
npm build succeeded in 5421ms
All builds succeeded!
Created commit: a7b3c2f - Agent Task task-001: Add XML documentation
Task completed successfully!

========================================
Execution Complete
Successful: 2/2
Failed: 0/2
========================================
```

## Next Steps

1. Start with small, simple tasks to test the agent
2. Review the git commits to verify changes
3. Gradually increase task complexity
4. Monitor the logs for any issues
5. Adjust task descriptions based on results

## Getting Help

If you encounter issues:
1. Check the console logs for detailed error messages
2. Review `.agent-state.json` to see task history
3. Examine git commits to see what was changed
4. Check backup files in `.agent-backups/` if needed
5. Review the README.md for additional documentation
