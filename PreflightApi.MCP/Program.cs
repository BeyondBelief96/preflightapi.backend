using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using PreflightApi.MCP.Services;
using PreflightApi.MCP.Tools;

var builder = Host.CreateApplicationBuilder(args);

// Configure the PreflightApi HTTP client
builder.Services.AddHttpClient<PreflightApiClient>(client =>
{
    var baseUrl = builder.Configuration["PreflightApi:BaseUrl"] ?? "https://localhost:7014";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);

    // Add gateway secret if configured (for authenticated API access)
    var gatewaySecret = builder.Configuration["PreflightApi:GatewaySecret"];
    if (!string.IsNullOrEmpty(gatewaySecret))
    {
        client.DefaultRequestHeaders.Add("X-Gateway-Secret", gatewaySecret);
    }
});

// Register MCP tools
builder.Services.AddSingleton<AirportTools>();
builder.Services.AddSingleton<WeatherTools>();
builder.Services.AddSingleton<FlightPlanningTools>();
builder.Services.AddSingleton<SafetyTools>();

// Configure MCP server
builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "preflight",
        Version = "1.0.0"
    };
})
.WithStdioServerTransport()
.WithToolsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

await app.RunAsync();
