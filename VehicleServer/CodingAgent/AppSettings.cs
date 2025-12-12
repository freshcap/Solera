namespace CodingAgent;

public class AppSettings
{
    public string AnthropicApiKey { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public string DotNetProjectPath { get; set; } = string.Empty;
    public string NpmProjectPath { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public string TasksFilePath { get; set; } = string.Empty;
}
