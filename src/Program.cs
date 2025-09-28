using Azure.Identity;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Session;
using Microsoft.SemanticKernel;
using SingleAgent.Agents;
using SingleAgent.Context;
using SingleAgent.Contracts;
using SingleAgent.Plubming;
using SingleAgent.Plumbing;
using SingleAgent.State;
using SingleAgent.Storage.Contract;
using SingleAgent.Storage.Providers;
using SingleAgent.Telemetry;
using SingleAgent.Tools;
using SingleAgent.Tools.SingleAgent.Tools;
using System;
using System.Diagnostics;
using System.Text;

// Declare logger outside try here for use in catch block
ILogger? logger = null; 

try
{
    // Build configuration to access user secrets and environment variables
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();

    // Determine environment (local or cloud)
    //var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
    //bool isLocal = environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
    var tenantIdOverride = configuration["tenant-id-override"];
    
    bool isLocalDev = configuration["ASPNETCORE_ENVIRONMENT"]?.Equals("Development", StringComparison.OrdinalIgnoreCase) ?? false;

    // Azure OpenAI configuration
    string openai_key = configuration["openai-key"] ?? throw new InvalidOperationException("Missing required secret: 'openai-key'.");
    string openai_endpoint = configuration["openai-endpoint"] ?? throw new InvalidOperationException("Missing required secret: 'openai-endpoint'.");

    // Inference deployment name
    string inference_deployment = configuration["inference-deployment"] ?? throw new InvalidOperationException("Missing required secret: 'inference-deployment'.");

    Console.WriteLine("Successfully loaded configuration secrets.");

    var builder = WebApplication.CreateBuilder(args);

    // Configure logging/telemetry for the application.
    var appInsightsConnectionString = configuration["application-insights"] ??
    throw new InvalidOperationException("Missing required secret: 'application-insights'");

    // For automatic logging: Enables auto-instrumentation for Application Insights telemetry
    // Enables automatic collection of telemetry data (requests, dependencies, exceptions, etc.) without writing code
    // Also enables the TelemetryClient for custom logging via ILogger
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });

    // Enable to route custom iLogger messages (_logger.Information(blah)) to Application Insights
    builder.Logging
        // Write logs to terminal and Visual Studio output window.
        .AddConsole()
        // Only log Information and above (Warning, Error, Critical).
        .SetMinimumLevel(LogLevel.Information)
        // Send logs to Azure Application Insights.
        .AddApplicationInsights(
            configureTelemetryConfiguration: config => config.ConnectionString = appInsightsConnectionString,
            configureApplicationInsightsLoggerOptions: options =>
            {
                // Ensures exceptions are tracked as exception telemetry
                options.TrackExceptionsAsExceptionTelemetry = true;
            }
        );

    // Remove the default Application Insights logging filter rule.
    // By default, Application Insights may filter out some log levels or categories.
    // This code finds and removes the default rule for the Application Insights logger provider,
    // allowing you to control log filtering explicitly elsewhere if needed.
    builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
    {
        var defaultRule = options.Rules.FirstOrDefault(rule =>
            rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
        if (defaultRule is not null)
        {
            options.Rules.Remove(defaultRule);
        }
    });

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        });

    // Retrieve required secrets from user secrets
    Console.WriteLine("Starting application...");

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    /// Configure Semantic Kernel
    var kernelBuilder = Kernel.CreateBuilder();

    // Address situation where application execution and user tenants are different
    // Local processing and application tenant ID is set - set tenant ID for DefaultAzureCredential
    if (isLocalDev)
    {
        Console.WriteLine("Environment is Local Dev");
        DefaultAzureCredentialOptions options = new();

        if (!string.IsNullOrEmpty(tenantIdOverride))
        {
            // Add tenant override ID as user user tenant is different from that of the host environment
            options = new DefaultAzureCredentialOptions
            {
                TenantId = tenantIdOverride
            };

            Console.WriteLine("User has added tenant ID Override value");
        };

        // Identity: Work in progress
        // See Claude projects: Identity

        ////var credential = new DefaultAzureCredential(options);

        ////var tokenRequestContext = new TokenRequestContext(new[] { "api://ea61d384-0c0c-4cd6-b30a-06d5690f15dd/.default" });

        ////// 3. Asynchronously obtain an access token from Azure AD
        //////    The credential will use the first successful auth method from step 1.
        ////AccessToken token = await credential.GetTokenAsync(tokenRequestContext);

        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: inference_deployment,
            endpoint: openai_endpoint,
            credentials: new DefaultAzureCredential(options)
        );
    }
    else
    {
        // running remote - use DefaultAzureCredential from environment
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: inference_deployment,
            endpoint: openai_endpoint,
            apiKey: openai_key
        );

        Console.WriteLine("User authenticated  with OpenAIKey");
    }

    // Register tools with the kernel
    kernelBuilder.Plugins.AddFromType<ClassifyIntentTool>();
    kernelBuilder.Plugins.AddFromType<UserValidationTool>();   
    kernelBuilder.Plugins.AddFromType<ProductValidationTool>();
    kernelBuilder.Plugins.AddFromType<CheckComplianceTool>();
    kernelBuilder.Plugins.AddFromType<JustifyApprovalTool>();

    //kernelBuilder.Services.AddScoped<TelemetryCollector>();
    // Add this line for the logger
    kernelBuilder.Services.AddLogging();

    // Register filter before building
    kernelBuilder.Services.AddSingleton<IFunctionInvocationFilter, TelemetryFunctionFilter>();
    kernelBuilder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    kernelBuilder.Services.AddScoped<IProductRepository, InMemoryProductRepository>();

    builder.Services.AddScoped<ContextPruningService>();
    //builder.Services.AddScoped<PurchaseStateReconstructor>();
 
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

    // DEBUG Code: List all registered plugins and functions (Semantic Kernel 1.17.2)
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

    ////// Debug code that determines logging level
    ////builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
    ////{
    ////    StringBuilder levels = new StringBuilder();

    ////    // Log all current filter rules for debugging
    ////    foreach (var rule in options.Rules)
    ////    {
    ////        levels.AppendLine($"Provider: {rule.ProviderName}, Category: {rule.CategoryName}, Level: {rule.LogLevel}");
    ////        //Console.WriteLine($"Provider: {rule.ProviderName}, Category: {rule.CategoryName}, Level: {rule.LogLevel}");
    ////        //var levels =+ ($"Provider: {rule.ProviderName}, Category: {rule.CategoryName}, Level: {rule.LogLevel}");
    ////    }

    ////    var allRules = levels.ToString();
    ////});

    // Middleware to add session ID to each logging entry to associate telemetry
    //app.UseMiddleware<SessionTrackingMiddleware>();
    // Another Claude disaster
    //builder.Services.AddHttpContextAccessor();
    //builder.Services.AddSingleton<ITelemetryInitializer, SessionTelemetryInitializer>();

    var app = builder.Build();

    // Middleware (should have stayed here)
    // Another Claude disaster
    // app.UseMiddleware<CustomSessionMiddleware>();

    logger = app.Services.GetRequiredService<ILogger<Program>>();

    // Configure the HTTP request pipeline.
    // Enable Swagger for demo purposes
    app.UseSwagger();
    app.UseSwaggerUI();

    // Comment out HTTPS redirection for Container Apps - ingress handles HTTPS
    // app.UseHttpsRedirection();

    app.UseSession();

    app.UseAuthorization();


    app.MapControllers();

    logger.LogInformation("Application started!");

    app.Run();

}
catch (Exception ex)
{
    // Handle and log startup exceptions
    Console.WriteLine($"Fatal error during start-up in program.cs: {ex}");
    logger?.LogCritical(ex, $"Fatal error during start-up in program.cs: {ex.Message}!");
    Environment.Exit(1);
    //throw new Exception($"Fatal error during start-up in program.cs: {ex.Message}", ex);
}
   
