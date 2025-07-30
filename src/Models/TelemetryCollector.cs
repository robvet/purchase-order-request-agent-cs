using System.Text.Json;

namespace SingleAgent.Models
{
    // TelemetryCollector: stores telemetry for the current request
    public class TelemetryCollector
    {
        private readonly List<string> _generalTelemetry = new();
        private readonly List<SimpleFunctionCall> _functionCalls = new();
        
        // Existing method for backward compatibility
        public void Add(string entry) => _generalTelemetry.Add(entry);
        public IReadOnlyList<string> GetAll() => _generalTelemetry;
        
        // Simplified function call tracking - just name and parameters
        public void RecordFunctionCall(string functionName, Dictionary<string, object> parameters)
        {
            _functionCalls.Add(new SimpleFunctionCall 
            { 
                Name = functionName, 
                Parameters = parameters, 
                Timestamp = DateTime.UtcNow 
            });
        }
        
        // NEW: Record function result/output
        public void RecordFunctionResult(string functionName, object? result)
        {
            var lastCall = _functionCalls.LastOrDefault(f => f.Name == functionName && f.Output == null);
            if (lastCall != null)
            {
                lastCall.Output = result?.ToString() ?? "";
            }
        }
        
        public void RecordFunctionError(string functionName, string error)
        {
            var lastCall = _functionCalls.LastOrDefault(f => f.Name == functionName && f.Output == null);
            if (lastCall != null)
            {
                lastCall.Output = $"ERROR: {error}";
            }
            _generalTelemetry.Add($"[ERROR] {functionName}: {error}");
        }
        
        public IReadOnlyList<SimpleFunctionCall> GetFunctionCalls() => _functionCalls;
        
        // Get clean execution summary for debugging
        public string GetExecutionSummary()
        {
            if (!_functionCalls.Any()) return "No function calls recorded";
            
            return string.Join(" → ", _functionCalls.Select(f => 
                $"{f.Name}({string.Join(", ", f.Parameters.Select(p => $"{p.Key}:{p.Value}"))})"
            ));
        }
        
        // Get detailed function call info for debugging
        public string GetDetailedExecutionLog()
        {
            if (!_functionCalls.Any()) return "No function calls recorded";
            
            var log = new List<string>();
            foreach (var call in _functionCalls)
            {
                log.Add($"[{call.Timestamp:HH:mm:ss.fff}] {call.Name}");
                if (call.Parameters.Any())
                {
                    foreach (var param in call.Parameters)
                    {
                        log.Add($"  └─ {param.Key}: {param.Value}");
                    }
                }
            }
            return string.Join(Environment.NewLine, log);
        }
        
        // NEW: Get formatted debug output with Input/Output structure as array
        public List<string> GetFormattedFunctionDebugOutput()
        {
            if (!_functionCalls.Any()) return new List<string> { "No function calls recorded" };
            
            var output = new List<string>();
            
            foreach (var call in _functionCalls)
            {
                // Add two empty lines before each function block for visual separation
                
                output.Add($"Function: {call.Name}");
                output.Add("Input:");
                
                if (call.Parameters.Any())
                {
                    foreach (var param in call.Parameters)
                    {
                        // Clean up \r\n escape sequences in parameter values
                        var cleanValue = param.Value?.ToString()?.Replace("\r\n", " | ").Replace("\n", " | ") ?? "";
                        output.Add($"  └─ {param.Key}: {cleanValue}");
                    }
                }
                else
                {
                    output.Add("  └─ (no parameters)");
                }
                
                output.Add("Output:");
                if (!string.IsNullOrEmpty(call.Output))
                {
                    // Try to format JSON output nicely
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(call.Output);
                        var formattedJson = JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true });
                        
                        // Add each line of the JSON as separate array elements
                        var lines = formattedJson.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                        foreach (var line in lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                output.Add($"  {line.TrimEnd()}");
                            }
                        }
                    }
                    catch
                    {
                        // If not valid JSON, just display as-is with indentation
                        output.Add($"  {call.Output}");
                    }
                }
                else
                {
                    output.Add("  (no output captured)");
                }
            }
            
            return output;
        }
    }

    public class SimpleFunctionCall
    {
        public string Name { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string? Output { get; set; } // NEW: Capture function output/result
    }
}
