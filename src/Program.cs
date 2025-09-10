using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using SingleAgent.Agents;
using SingleAgent.Contracts;
using SingleAgent.Models;
using SingleAgent.Models.DTO;
using SingleAgent.Prompting;
using SingleAgent.Storage.Contract;
using SingleAgent.Storage.Providers;
using SingleAgent.Telemetry;
using SingleAgent.Tools;
using SingleAgent.Uiltities;
using SingleAgent.Utlls;
using System.Diagnostics;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Build configuration to access user secrets and environment variables
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();

    // Configure logging
    builder.Services.AddLogging(config =>
    {
        config.AddConsole();
        config.SetMinimumLevel(LogLevel.Information); // Changed from Error to Information
    });

    // Retrieve required secrets from user secrets
    Console.WriteLine("Starting application...");

    // Azure OpenAI configuration
    string openai_key = configuration["openai-key"] ?? throw new InvalidOperationException("Missing required secret: 'openai-key'.");
    string openai_endpoint = configuration["openai-endpoint"] ?? throw new InvalidOperationException("Missing required secret: 'openai-endpoint'.");

    // Inference deployment name
    string inference_deployment = configuration["inference-deployment"] ?? throw new InvalidOperationException("Missing required secret: 'inference-deployment'.");

    Console.WriteLine("Successfully loaded configuration secrets.");

    /// Configure Semantic Kernel
    var kernelBuilder = Kernel.CreateBuilder();

    //kernelBuilder.AddAzureOpenAIChatCompletion(
    //    deploymentName: deployment,
    //    endpoint: endpoint,
    //    apiKey: key
    //);

    // Determine environment (local or cloud)
    //var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
    //bool isLocal = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);

    var applicationTenantId = configuration["application-tenant"] ?? throw new ArgumentException("Application TenantId is missing");
    bool isLocalDev = configuration["ASPNETCORE_ENVIRONMENT"]?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;

    // Address situation where application execution and user tenants are different
    // Local processing and application tenant ID is set - set tenant ID for DefaultAzureCredential
    if (isLocalDev && !string.IsNullOrEmpty(applicationTenantId))
    {
        // Use managed identity in cloud
        var options = new DefaultAzureCredentialOptions
        {
            TenantId = applicationTenantId
        };

        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: inference_deployment,
            endpoint: openai_endpoint,
            credentials: new DefaultAzureCredential(options)
        );
    }
    // Local processing and application tenant ID is NOT set - use API Key
    else if (isLocalDev)
    {
        kernelBuilder.AddAzureOpenAIChatCompletion(
           deploymentName: inference_deployment,
           endpoint: openai_endpoint,
           apiKey: openai_key
       );
    }
    else
    {
        // running remote - use DefaultAzureCredential from environment
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: inference_deployment,
            endpoint: openai_endpoint,
            credentials: new DefaultAzureCredential()
        );
    }

    //kernelBuilder.AddAzureOpenAIChatCompletion(
    //    deploymentName: deployment,
    //    endpoint: endpoint,
    //    apiKey: key
    //);


    // Register tools with the kernel
    kernelBuilder.Plugins.AddFromType<ClassifyIntentTool>();
    kernelBuilder.Plugins.AddFromType<ValidateProductTool>();   
    kernelBuilder.Plugins.AddFromType<ExtractDetailsTool>();
    kernelBuilder.Plugins.AddFromType<CheckComplianceTool>();
    kernelBuilder.Plugins.AddFromType<JustifyApprovalTool>();

    //kernelBuilder.Plugins.AddFromType<ClassifyRequestTool>();
    //kernelBuilder.Plugins.AddFromType<ShowQualifiedProductsTool>();
    //kernelBuilder.Plugins.AddFromType<SecondChoiceOptimizerTool>();
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

    //builder.Services.AddSwaggerGen(c =>
    //{
    //    c.OperationFilter<AddShowDebugHeaderParameter>();
    //});

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
    // Enable Swagger for demo purposes
    app.UseSwagger();
    app.UseSwaggerUI();

    // Comment out HTTPS redirection for Container Apps - ingress handles HTTPS
    // app.UseHttpsRedirection();

    app.UseSession();

    app.UseAuthorization();


    app.MapControllers();

    app.Run();

}
catch (Exception ex)
{
    Console.WriteLine($"Fatal error during start-up in program.cs: {ex}");
    throw new Exception($"Fatal error during start-up in program.cs: {ex.Message}", ex);
}
   
