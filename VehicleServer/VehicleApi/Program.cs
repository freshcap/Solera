using VehicleApi;
using VehicleApi.Clients;
using VehicleApi.Data;
using VehicleApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure CORS to accept anything
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure AppSettings with environment variable support
builder.Services.Configure<AppSettings>(options =>
{
    options.DbConnectionString = builder.Configuration["SoleraConnectionString"] 
        ?? throw new InvalidOperationException("SoleraConnectionString environment variable missing");
    options.AnthropicApiKey = builder.Configuration["ANTHROPIC_API_KEY"]
        ?? throw new InvalidOperationException("ANTHROPIC_API_KEY environment variable missing");
});

// Register repository
builder.Services
    .AddTransient<IVehicleRepository, VehicleRepository>()
    .AddTransient<IClaudeClient, ClaudeClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors(); // Must be placed before UseHttpsRedirection and UseAuthorization
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
