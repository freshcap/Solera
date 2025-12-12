using CodingAgent.Orchestration;
using CodingAgent.Services;
using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CodingAgent;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Parse command line arguments
        var dryRun = args.Contains("--dry-run");

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        // Setup dependency injection
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Configure AppSettings with environment variable support
        services.Configure<AppSettings>(options =>
        {
            options.AnthropicApiKey = configuration["ANTHROPIC_API_KEY"]
                ?? throw new InvalidOperationException("ANTHROPIC_API_KEY environment variable not set");
            options.WorkingDirectory = configuration["Agent:WorkingDirectory"]
                ?? Directory.GetCurrentDirectory();
            options.DotNetProjectPath = configuration["Agent:DotNetProjectPath"]
                ?? "VehicleApi/VehicleApi.csproj";
            options.NpmProjectPath = configuration["Agent:NpmProjectPath"]
                ?? "../VehicleWebapp";
            options.MaxRetries = int.Parse(configuration["Agent:MaxRetries"] ?? "3");
            options.TasksFilePath = configuration["Agent:TasksFilePath"]
                ?? "tasks.json";
        });

        // Get settings for use in service registration
        var settings = new AppSettings();
        configuration.GetSection("Agent").Bind(settings);
        settings.AnthropicApiKey = configuration["ANTHROPIC_API_KEY"]
            ?? throw new InvalidOperationException("ANTHROPIC_API_KEY environment variable not set");

        var workingDirectory = settings.WorkingDirectory != string.Empty
            ? settings.WorkingDirectory
            : Directory.GetCurrentDirectory();
        var dotnetProjectPath = settings.DotNetProjectPath;
        var npmProjectPath = settings.NpmProjectPath;
        var maxRetries = settings.MaxRetries;
        var tasksFilePath = settings.TasksFilePath;

        // Register services
        services.AddSingleton<IClaudeClient>(sp => new ClaudeClientBase(settings.AnthropicApiKey));
        services.AddSingleton<IFileSystemManager>(sp =>
            new FileSystemManager(
                sp.GetRequiredService<ILogger<FileSystemManager>>(),
                workingDirectory));
        services.AddSingleton<IGitManager>(sp =>
            new GitManager(
                sp.GetRequiredService<ILogger<GitManager>>(),
                workingDirectory));
        services.AddSingleton<IBuildRunner>(sp =>
            new BuildRunner(
                sp.GetRequiredService<ILogger<BuildRunner>>(),
                workingDirectory));
        services.AddSingleton<IPromptBuilder, PromptBuilder>();
        services.AddSingleton<IResponseParser, ResponseParser>();
        services.AddSingleton<IRateLimiter, RateLimiter>();
        services.AddSingleton<IStateManager>(sp =>
            new StateManager(
                sp.GetRequiredService<ILogger<StateManager>>(),
                workingDirectory));
        services.AddSingleton<ITaskLoader, TaskLoader>();
        services.AddSingleton<IFileDiscoveryService>(sp =>
            new FileDiscoveryService(
                sp.GetRequiredService<IClaudeClient>(),
                sp.GetRequiredService<IFileSystemManager>(),
                sp.GetRequiredService<ILogger<FileDiscoveryService>>(),
                workingDirectory));
        services.AddSingleton<IDiffPreviewService, DiffPreviewService>();
        services.AddSingleton<IApprovalService, ApprovalService>();
        services.AddSingleton(sp =>
            new AgentOrchestrator(
                sp.GetRequiredService<IClaudeClient>(),
                sp.GetRequiredService<IFileSystemManager>(),
                sp.GetRequiredService<IGitManager>(),
                sp.GetRequiredService<IBuildRunner>(),
                sp.GetRequiredService<IPromptBuilder>(),
                sp.GetRequiredService<IResponseParser>(),
                sp.GetRequiredService<IRateLimiter>(),
                sp.GetRequiredService<IStateManager>(),
                sp.GetRequiredService<IFileDiscoveryService>(),
                sp.GetRequiredService<IDiffPreviewService>(),
                sp.GetRequiredService<IApprovalService>(),
                sp.GetRequiredService<ILogger<AgentOrchestrator>>(),
                dotnetProjectPath,
                npmProjectPath,
                maxRetries,
                dryRun));

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Get logger
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("========================================");
            logger.LogInformation("Autonomous Coding Agent Starting");
            logger.LogInformation("========================================");
            logger.LogInformation("Working Directory: {WorkingDirectory}", workingDirectory);
            logger.LogInformation(".NET Project: {DotNetProject}", dotnetProjectPath);
            logger.LogInformation("npm Project: {NpmProject}", npmProjectPath);
            logger.LogInformation("Max Retries: {MaxRetries}", maxRetries);

            if (dryRun)
            {
                logger.LogWarning("DRY RUN MODE ENABLED - No changes will be applied");
            }

            logger.LogInformation("========================================");

            // Load state
            var stateManager = serviceProvider.GetRequiredService<IStateManager>();
            var state = await stateManager.LoadState();

            // Load tasks
            var taskLoader = serviceProvider.GetRequiredService<ITaskLoader>();
            var tasks = await taskLoader.LoadTasks(tasksFilePath);

            if (tasks.Count == 0)
            {
                logger.LogError("No tasks found in {TasksFile}", tasksFilePath);
                return 1;
            }

            logger.LogInformation("Loaded {TaskCount} tasks", tasks.Count);

            // Get orchestrator
            var orchestrator = serviceProvider.GetRequiredService<AgentOrchestrator>();

            // Execute tasks
            var successCount = 0;
            var failCount = 0;

            foreach (var task in tasks)
            {
                // Check if task was already completed
                if (state.CompletedTasks.Any(t => t.TaskId == task.Id && t.Success))
                {
                    logger.LogInformation("Task {TaskId} already completed. Skipping.", task.Id);
                    successCount++;
                    continue;
                }

                var success = await orchestrator.ExecuteTask(task);

                if (success)
                {
                    successCount++;
                }
                else
                {
                    failCount++;
                }

                // Small delay between tasks
                await Task.Delay(1000);
            }

            logger.LogInformation("========================================");
            logger.LogInformation("Execution Complete");
            logger.LogInformation("Successful: {SuccessCount}/{Total}", successCount, tasks.Count);
            logger.LogInformation("Failed: {FailCount}/{Total}", failCount, tasks.Count);
            logger.LogInformation("========================================");

            return failCount > 0 ? 1 : 0;
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Fatal error occurred");
            return 1;
        }
    }
}
