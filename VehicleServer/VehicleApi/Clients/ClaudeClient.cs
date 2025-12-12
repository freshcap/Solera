namespace VehicleApi.Clients;

using Common;
using Microsoft.Extensions.Options;


public class ClaudeClient(IOptions<AppSettings> settings) 
    : ClaudeClientBase(settings.Value.AnthropicApiKey)
{
}
