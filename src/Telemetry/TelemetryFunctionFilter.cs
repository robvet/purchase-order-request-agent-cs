using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SingleAgent.Models;
using SingleAgent.Storage.Contract;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace SingleAgent.Telemetry
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
            if (telemetryCollector == null) 
            {
                await next(context);
                return;
            }

            var function = context.Function;
            
            // Extract parameters for telemetry - simple and clean
            var parameters = context.Arguments?.ToDictionary(kvp => kvp.Key, kvp => (object)(kvp.Value?.ToString() ?? "")) 
                           ?? new Dictionary<string, object>();
            
            // Record function call with parameters - this is the key debugging info
            telemetryCollector.RecordFunctionCall(function.Name, parameters);
            
            // Keep simple backward compatible telemetry
            telemetryCollector.Add($"[FUNCTION_CALL] {function.Name}");
            
            _logger.LogInformation("Function Starting: {FunctionName}", function.Name);

            try
            {
                // Execute the function
                await next(context);
                
                // NEW: Capture function result/output
                var result = context.Result?.GetValue<object>();
                telemetryCollector.RecordFunctionResult(function.Name, result);
                
                _logger.LogInformation("Function Completed: {FunctionName}", function.Name);
            }
            catch (Exception ex)
            {
                // Record error for debugging
                telemetryCollector.RecordFunctionError(function.Name, ex.Message);
                
                _logger.LogError(ex, "Function Failed: {FunctionName}", function.Name);
                throw;
            }
        }
    }
}


//var function = context.Function;
//var telemetryEntry = $"[FUNCTION CALL] {function.PluginName}.{function.Name} | Desc: {function.Description} | Params: " +
//    (context.Arguments.Any() ? string.Join(", ", context.Arguments.Select(a => $"{a.Key}: {a.Value}")) : "(No parameters)");

//_telemetryCollector.Add(telemetryEntry);
//_logger.LogInformation($"Telemetry added: {telemetryEntry}");

//await next(context); // Execute the function

//_logger.LogInformation($"Function {function.Name} completed");