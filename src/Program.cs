using Microsoft.SemanticKernel;
using NearbyCS_API.Agents;
using NearbyCS_API.Models;
using NearbyCS_API.Storage.Contract;
using NearbyCS_API.Storage.Providers;
using NearbyCS_API.Telemetry;
using NearbyCS_API.Utlls;
using System.Diagnostics;
using NearbyCS_API.Prompting;
using NearbyCS_API.Contracts;
using NearbyCS_API.Models.DTO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Build configuration to access user secrets
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

// Configure logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Error);
});

// Retrieve required secrets from user secrets
string openAIApiKey = configuration["openai-apikey"] ?? throw new InvalidOperationException("Missing required secret: 'openai-apikey'.");
string deploymentName = configuration["openai-deploymentname"] ?? throw new InvalidOperationException("Missing required secret: 'openai-deploymentname'.");
string endpoint = configuration["openai-endpoint"] ?? throw new InvalidOperationException("Missing required secret: 'openai-endpoint'.");

// Configure Semantic Kernel
var kernelBuilder = Kernel.CreateBuilder();

kernelBuilder.AddAzureOpenAIChatCompletion(
    deploymentName: deploymentName,
    endpoint: endpoint,
    apiKey: openAIApiKey
);

// Register tools with the kernel
kernelBuilder.Plugins.AddFromType<ClassifyRequestTool>();
kernelBuilder.Plugins.AddFromType<CheckPolicyComplianceTool>();
//kernelBuilder.Plugins.AddFromType<SubmitToERPTool>();


//kernelBuilder.Services.AddScoped<TelemetryCollector>();
// Add this line for the logger
kernelBuilder.Services.AddLogging();
// Register filter before building
kernelBuilder.Services.AddSingleton<IFunctionInvocationFilter, TelemetryFunctionFilter>();

kernelBuilder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

kernelBuilder.Services.AddScoped<IProductRepository, InMemoryProductRepository>();

var kernel = kernelBuilder.Build();

// Register Kernel as singleton
builder.Services.AddSingleton(kernel);

// <Warning>
/// Do not register IStateStore as Singleton. 
/// Each user will have their own state.
/// As a Singleton, all users will share the same state value leading to concurrency (overwriting) issues and data leaks.
/// Set as Scoped, which is a separate instance for each HTTP Request.
/// Doing so, we're not sharing a single object with different values across multiple users.
/// </Warning>
// Repository Pattern for State
builder.Services.AddScoped<IStateStore, InMemorySessionStateStore>();
// Repository Pattern for Data
builder.Services.AddScoped<IProductRepository, InMemoryProductRepository>();


// DEBUG: List all registered plugins and functions (Semantic Kernel 1.17.2)
//foreach (var plugin in kernel.Plugins)
//{
//    Debug.WriteLine($"Plugin: {plugin.Name}");
//    foreach (var function in plugin)
//    {
//        Debug.WriteLine($"  Function: {function.Name}");
//    }
//}

// Add services to the container.

// Register Distributed Memory Cache
builder.Services.AddDistributedMemoryCache();

// Register HttpContextAccessor as a singleton
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// TelemetryCollector: stores telemetry for the current request
builder.Services.AddScoped<TelemetryCollector>();

// Must also make NearbyAgent scoped. Cannot make Singleton as a Singleton cannot have dependency on a scoped service
//builder.Services.AddScoped<NearbyAgent>();
builder.Services.AddSession();

//builder.Services.AddScoped<NearbyAgent>();
builder.Services.AddScoped<IPurchaseOrderAgent, PurchaseOrderAgent>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();