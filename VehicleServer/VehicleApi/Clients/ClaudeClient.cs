namespace VehicleApi.Clients;

using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text;
using System.Text.Json;

public interface IClaudeClient
{
    Task<string> GetCompletion(string prompt);
}

public class ClaudeClient : IClaudeClient
{
    private readonly HttpClient _client;
    private readonly string _apiKey;

    public ClaudeClient(IOptions<AppSettings> settings)
    {
        _apiKey = settings.Value.AnthropicApiKey;
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> GetCompletion(string prompt)
    {
        var request = new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 2000,
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync(
            "https://api.anthropic.com/v1/messages",
            content
        );

        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ClaudeResponse>(responseJson);

        return result.content[0].text;
    }
}

public class ClaudeRequest
{
    public string Prompt { get; set; }
}

public class ClaudeResponse
{
    public Content[] content { get; set; }
}

public class Content
{
    public string text { get; set; }
}
