Side-by-side model comparison tool.

SLM for classification, summarization, etc

Fix parameter validation in tools

return JsonSerializer.Serialize(new
{
    toolname = ToolName,
    status = "error",
    error = "invalid_parameters",
    message = "All parameters are required and must be valid",
    invalid_parameters = new[] {          // More structured way to show what's invalid
        string.IsNullOrEmpty(justification) ? "justification" : null,
        string.IsNullOrEmpty(item) ? "item" : null,
        cost <= 0 ? "cost" : null,
        string.IsNullOrEmpty(reasoning) ? "reasoning" : null
    }.Where(p => p != null).ToArray(),    // Only include invalid parameters
    timestamp = DateTime.UtcNow
});


