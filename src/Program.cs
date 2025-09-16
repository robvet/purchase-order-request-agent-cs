using Azure.Identity;
using Microsoft.SemanticKernel;
using SingleAgent.Agents;
using SingleAgent.Context;
using SingleAgent.Contracts;
using SingleAgent.Models;
using SingleAgent.State;
using SingleAgent.Storage.Contract;
using SingleAgent.Storage.Providers;
using SingleAgent.Telemetry;
using SingleAgent.Tools;
using SingleAgent.Tools.SingleAgent.Tools;
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

    // Configure logging for the application.
    // Retrieve Application Insights connection string from user secrets
    var appInsightsConnectionString = configuration["application-insights"] ?? throw new InvalidOperationException("Missing required secret: 'ApplicationInsights:ConnectionString'."); ;


    // This block sets up both local console logging and Application Insights logging (if a connection string is provided).
    builder.Services.AddLogging(config =>
    {
        // Add logging output to the local console view. This is useful for local development and debugging,
        // as log messages will appear in the terminal or Visual Studio output window.
        config.AddConsole();

        // Set the minimum log level to Information.
        // This means only log messages with severity Information or higher (Warning, Error, Critical) will be recorded.
        // Debug and Trace level logs will be ignored unless this is set lower.

        // LogLevel options (from most to least diagnostic traffic):
        // Verbose(0) – very chatty diagnostics for deep debugging; avoid in prod except short bursts.
        // Information(1) – normal app lifecycle and business events(start / stop, key state changes).
        // Warning(2) – abnormal / transient conditions(retries, partial failures, degraded paths).
        // Error(3) – operation failed or handled exception; requires investigation.
        // Critical(4) – service / app failure or data loss; page someone.
        config.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);

        // If an Application Insights connection string is available, configure Application Insights logging.
        // Application Insights is the cloud-based telemetry and monitoring service from Azure.
        // It allows you to collect, analyze, and act on telemetry data from your application.
        if (!string.IsNullOrEmpty(appInsightsConnectionString))
        {
            config.AddApplicationInsights(

                // Activate telemetry configuration with connection string to send EXPLICIT "LOGGING" messages (via iLogger commands).
                // Know that some auto-instrumentation supported by .NET: Request logging, dependency tracking, and exception logging.
                // Python auto-instrumentation not supported at this time.
                configureTelemetryConfiguration: telemetryConfig =>
                {
                    telemetryConfig.ConnectionString = appInsightsConnectionString;
                },

                // Optionally configure Application Insights logger options.
                // In this case, lambda specifies that no additional options are set.
                configureApplicationInsightsLoggerOptions: _ => { }

                // Uncomment the following block to customize logging filters.
                //configureApplicationInsightsLoggerOptions: options =>
                //{
                //    // Only log warnings and above for Microsoft categories
                //    options.FilterLogCategories.Add("Microsoft", Microsoft.Extensions.Logging.LogLevel.Warning);

                //    // Only log errors and above for System categories
                //    options.FilterLogCategories.Add("System", Microsoft.Extensions.Logging.LogLevel.Error);

                //    // You can also set a global minimum level
                //    options.Filter = (category, logLevel) =>
                //    {
                //        // Example: Only log Information and above for your app's namespace
                //        if (category.StartsWith("SingleAgent"))
                //            return logLevel >= Microsoft.Extensions.Logging.LogLevel.Information;
                //        return true;
                //    };
                //}


            );
        }
    });

    // Add Application Insights telemetry to provide monitoring and logging
    builder.Services.AddApplicationInsightsTelemetry();

    builder.Logging
     .AddApplicationInsights(
         configureTelemetryConfiguration: config =>
         {
             config.ConnectionString = configuration["application-insights"];
             // optional: config.TelemetryInitializers.Add(...) etc
         },
         configureApplicationInsightsLoggerOptions: options =>
         {
             // optional: include scopes, configure exceptions
             options.TrackExceptionsAsExceptionTelemetry = true;
         }
     );


    builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
    {
        var defaultRule = options.Rules.FirstOrDefault(rule =>
            rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");
        if (defaultRule is not null)
        {
            options.Rules.Remove(defaultRule);
        }
    });


    //// Adaptive Sampling to limit the volume of telemetry sent to Application Insights.
    //// Register Application Insights telemetry services with the dependency injection container.
    //builder.Services.Configure<TelemetryConfiguration>(config =>
    //{
    //    var chain = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
    //    chain.UseAdaptiveSampling(
    //        maxTelemetryItemsPerSecond: 5,
    //        excludedTypes: "Exception" // keep all exceptions (see caveat below)
    //    );
    //    chain.Build();
    //});


    // Fixed-rate Sampling to further reduce telemetry volume.
    //// Register Application Insights telemetry services with the dependency injection container.
    //builder.Services.Configure<TelemetryConfiguration>(config =>
    //{
    //    var chain = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
    //    chain.UseSampling(samplingPercentage: 10.0); // ~10%
    //    chain.Build();
    //});

    //// Hybrid approach: Adaptive Sampling + Fixed-rate Sampling
    //// Register Application Insights telemetry services with the dependency injection container.
    //builder.Services.Configure<TelemetryConfiguration>(config =>
    //{
    //    config.DisableTelemetry = false;

    //    // Configure adaptive sampling with custom percentage
    //    var chainBuilder = config.DefaultTelemetrySink.TelemetryProcessorChainBuilder;
    //    chainBuilder.UseAdaptiveSampling(maxTelemetryItemsPerSecond: 5);
    //    chainBuilder.UseSampling(samplingPercentage: 25.0);
    //    chainBuilder.Build();
    //});



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

    builder.Services.AddScoped<ContextPruningService>();
    builder.Services.AddScoped<PurchaseStateReconstructor>();
 
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



    builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
    {
        StringBuilder levels = new StringBuilder();

        // Log all current filter rules for debugging
        foreach (var rule in options.Rules)
        {
            levels.AppendLine($"Provider: {rule.ProviderName}, Category: {rule.CategoryName}, Level: {rule.LogLevel}");
            //Console.WriteLine($"Provider: {rule.ProviderName}, Category: {rule.CategoryName}, Level: {rule.LogLevel}");
            //var levels =+ ($"Provider: {rule.ProviderName}, Category: {rule.CategoryName}, Level: {rule.LogLevel}");
        }

        var allRules = levels.ToString();
    });






    var app = builder.Build();

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
   
