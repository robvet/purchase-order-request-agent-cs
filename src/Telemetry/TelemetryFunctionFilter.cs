using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NearbyCS_API.Models;
using NearbyCS_API.Storage.Contract;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace NearbyCS_API.Telemetry
{
    public class TelemetryFunctionFilter : IFunctionInvocationFilter
    {
        private readonly ILogger<TelemetryFunctionFilter> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerOptions _jsonOptions;

        public TelemetryFunctionFilter(
            ILogger<TelemetryFunctionFilter> logger, 
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            var telemetryCollector = _httpContextAccessor.HttpContext?.Items["TelemetryCollector"] as TelemetryCollector;
            if (telemetryCollector == null) return;

            var function = context.Function;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // DEMO MODE: Keep it simple - just log to console, don't bloat chat history
            _logger.LogInformation("Tool Starting: {ToolName}", function.Name);

            try
            {
                // Execute the function
                await next(context);

                stopwatch.Stop();
                
                // DEMO MODE: Simple logging only - no chat history bloat
                _logger.LogInformation("Tool Completed: {ToolName} in {Duration}ms", 
                    function.Name, stopwatch.ElapsedMilliseconds);

                // Only add minimal telemetry to chat context for demo
                telemetryCollector.Add($"[TOOL] {function.Name} completed");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Tool Failed: {ToolName} after {Duration}ms", 
                    function.Name, stopwatch.ElapsedMilliseconds);
                
                // Add simple error info
                telemetryCollector.Add($"[TOOL_ERROR] {function.Name} failed");
                throw;
            }
        }

        // TODO: FUTURE COMPLEXITY - Enterprise telemetry for production debugging
        // The original verbose telemetry code has been commented out to prevent chat history bloat
        // Uncomment and restore when enterprise-level debugging is needed
    }
}


//var function = context.Function;
//var telemetryEntry = $"[FUNCTION CALL] {function.PluginName}.{function.Name} | Desc: {function.Description} | Params: " +
//    (context.Arguments.Any() ? string.Join(", ", context.Arguments.Select(a => $"{a.Key}: {a.Value}")) : "(No parameters)");

//_telemetryCollector.Add(telemetryEntry);
//_logger.LogInformation($"Telemetry added: {telemetryEntry}");

//await next(context); // Execute the function

//_logger.LogInformation($"Function {function.Name} completed");