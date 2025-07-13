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
        private readonly IStateStore _stateStore;
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

            // Get session ID from HttpContext if available
            string sessionId = _httpContextAccessor.HttpContext?.Session?.Id ?? "default-session";

            var function = context.Function;
            var startTime = DateTime.UtcNow;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Format function parameters for better readability
            string formattedParams = FormatFunctionParameters(context.Arguments);

            // BEFORE - Capture function call details in a more structured way
            var toolCallEntry = new
            {
                Type = "ToolCall",
                ToolName = $"{function.PluginName}.{function.Name}",
                Description = function.Description,
                Parameters = context.Arguments.ToDictionary(a => a.Key, a => a.Value),
                StartTime = startTime.ToString("HH:mm:ss.fff")
            };

            // Add structured tool call entry
            var toolCallJson = JsonSerializer.Serialize(toolCallEntry, _jsonOptions);
            telemetryCollector.Add($"[TOOL_CALL] {toolCallJson}");

            // Add human-readable entry for logging
            var beforeEntry = $"[FUNCTION START] {function.PluginName}.{function.Name} | " +
                $"Desc: {function.Description} | " +
                $"Params: {formattedParams} | " +
                $"StartTime: {startTime:HH:mm:ss.fff}";

            _logger.LogInformation($"Telemetry added: {beforeEntry}");

            // Track if function succeeds or fails
            bool functionSucceeded = false;
            string errorMessage = string.Empty;
            object? result = null;

            try
            {
                // Execute the function
                await next(context);
                functionSucceeded = true;

                // Try to capture the result if available
                if (context.Result != null)
                {
                    result = context.Result.GetValue<object>();
                }
            }
            catch (Exception ex)
            {
                functionSucceeded = false;
                errorMessage = ex.Message;
                _logger.LogError(ex, "Function execution failed: {FunctionName}", function.Name);
                throw; // Re-throw to maintain error handling
            }
            finally
            {
                // AFTER - Capture function completion details
                stopwatch.Stop();
                var endTime = DateTime.UtcNow;

                string formattedResult = FormatToolResult(result);

                // Create structured result entry
                var toolResultEntry = new
                {
                    Type = "ToolResult",
                    ToolName = $"{function.PluginName}.{function.Name}",
                    Status = functionSucceeded ? "SUCCESS" : "FAILED",
                    Duration = $"{stopwatch.ElapsedMilliseconds}ms",
                    EndTime = endTime.ToString("HH:mm:ss.fff"),
                    Result = functionSucceeded ? formattedResult : null,
                    Error = functionSucceeded ? null : errorMessage
                };

                // Add structured tool result entry
                var toolResultJson = JsonSerializer.Serialize(toolResultEntry, _jsonOptions);
                telemetryCollector.Add($"[TOOL_RESULT] {toolResultJson}");

                // Add human-readable entry for logging
                var afterEntry = $"[FUNCTION END] {function.PluginName}.{function.Name} | " +
                    $"Status: {(functionSucceeded ? "SUCCESS" : "FAILED")} | " +
                    $"Duration: {stopwatch.ElapsedMilliseconds}ms | " +
                    $"EndTime: {endTime:HH:mm:ss.fff}";

                if (functionSucceeded && result != null)
                {
                    string jsonResult = null;

                    // Try to parse as JSON if it's a string
                    if (result is string strResult && IsJson(strResult))
                    {
                        try
                        {
                            // If it's JSON, format it nicely
                            var jsonElement = JsonSerializer.Deserialize<JsonElement>(strResult);
                            var formatted = JsonSerializer.Serialize(jsonElement, _jsonOptions);

                            // Save the JSON result for state updates
                            jsonResult = strResult;

                            // Add as a separate entry for clarity
                            telemetryCollector.Add($"[TOOL_JSON_RESULT] {function.Name}: {formatted}");

                            afterEntry += " | Result: [JSON data captured separately]";
                        }
                        catch (Exception ex)
                        {
                            // If JSON parsing fails, include as string
                            var truncated = TruncateString(strResult, 100);
                            afterEntry += $" | Result: {truncated}";
                            _logger.LogError(ex, "Failed to process JSON result or update state");
                        }
                    }
                    else
                    {
                        // For non-JSON results
                        var resultPreview = result.ToString();
                        if (!string.IsNullOrEmpty(resultPreview))
                        {
                            var truncated = TruncateString(resultPreview, 100);
                            afterEntry += $" | Result: {truncated}";
                        }
                    }
                }
                else if (!functionSucceeded)
                {
                    afterEntry += $" | Error: {errorMessage}";
                }

                telemetryCollector.Add(afterEntry);
                _logger.LogInformation($"Telemetry added: {afterEntry}");
            }
        }

        private string FormatFunctionParameters(KernelArguments arguments)
        {
            if (arguments == null || !arguments.Any()) return "(No parameters)";

            var paramsList = new List<string>();
            foreach (var arg in arguments)
            {
                string valueStr;
                if (arg.Value == null)
                {
                    valueStr = "null";
                }
                else if (arg.Value is string strValue)
                {
                    valueStr = $"\"{TruncateString(strValue, 50)}\"";
                }
                else
                {
                    valueStr = arg.Value.ToString() ?? "null";
                }

                paramsList.Add($"{arg.Key}: {valueStr}");
            }

            return string.Join(", ", paramsList);
        }

        private string FormatToolResult(object? result)
        {
            if (result == null) return null;

            // If it's a string that looks like JSON, try to format it nicely
            if (result is string strResult && IsJson(strResult))
            {
                try
                {
                    // Parse and reformat the JSON string for better readability
                    using (var doc = JsonDocument.Parse(strResult))
                    {
                        return JsonSerializer.Serialize(doc.RootElement, _jsonOptions);
                    }
                }
                catch
                {
                    // If parsing fails, just return the original string
                    return strResult;
                }
            }

            // For non-string or non-JSON results
            return result.ToString();
        }

        private string FormatJsonForDisplay(string jsonString)
        {
            try
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonString);
                return JsonSerializer.Serialize(jsonElement, _jsonOptions);
            }
            catch
            {
                return jsonString; // Return original if parsing fails
            }
        }

        private string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : $"{value.Substring(0, maxLength)}...";
        }

        private bool IsJson(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            value = value.Trim();
            return (value.StartsWith("{") && value.EndsWith("}"))
                || (value.StartsWith("[") && value.EndsWith("]"));
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