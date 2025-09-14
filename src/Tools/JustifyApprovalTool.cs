using Microsoft.SemanticKernel;
using SingleAgent.Tools.SingleAgent.Tools;
using System.ComponentModel;
using System.Text.Json;

namespace SingleAgent.Tools
{
    public class JustifyApprovalTool
    {
        private const string ToolName = "JustifyApprovalTool";
        private readonly ILogger<JustifyApprovalTool> _logger;

        public JustifyApprovalTool(ILogger<JustifyApprovalTool> logger)
        {
            _logger = logger;
        }

        [KernelFunction]
        [Description("Evaluates justification for hardware purchases that exceed the $1000 cost limit")]
        public async Task<string> EvaluateJustificationAsync(
            Kernel kernel,
            [Description("The justification provided by the user for exceeding the cost limit")] string justification,
            [Description("The requested hardware item that exceeds the limit")] string item,
            [Description("The cost that exceeds the $1000 limit")] decimal cost,
            [Description("(REQUIRED) Model's explanation for why justify approval is needed at this step")] string reasoning)
        {
            try
            {
                _logger.LogInformation("Processing justification in {ToolName}: justification {justification}", ToolName, justification);
                _logger.LogInformation("Processing item in {ToolName}:item {item}", ToolName, item);
                _logger.LogInformation("Processing cost in {ToolName}:cost {cost}", ToolName, cost);
                _logger.LogInformation("Processing reasoning in {ToolName} with reasoning: {Reasoning}", ToolName, reasoning);


                // validate parameters  
                if (string.IsNullOrWhiteSpace(justification) || string.IsNullOrWhiteSpace(item) ||
                    cost <= 0 || string.IsNullOrWhiteSpace(reasoning))
                {
                    return JsonSerializer.Serialize(new
                    {
                        toolname = ToolName,
                        status = "error",
                        error = "invalid_parameters",
                        message = "All parameters are required and must be valid",
                        invalid_parameters = new[] {
                            string.IsNullOrEmpty(justification) ? "justification" : null,
                            string.IsNullOrEmpty(item) ? "item" : null,
                            cost <= 0 ? "cost" : null,
                            string.IsNullOrEmpty(reasoning) ? "reasoning" : null
                        }.Where(p => p != null).ToArray(), // only show invalid params
                        timestamp = DateTime.UtcNow
                    });
                }

                var prompt = JustificationPrompt
                    .Replace("{{justification}}", justification)
                    .Replace("{{item}}", item)
                    .Replace("{{cost}}", cost.ToString("C"));

                var result = await kernel.InvokePromptAsync(prompt, new() {
                    { "justification", justification },
                    { "item", item },
                    { "cost", cost.ToString("C") }
                });

                string rawJson = result.ToString();

                try
                {
                    using var doc = JsonDocument.Parse(rawJson);
                    var root = doc.RootElement;

                    // Validate that the response has the expected structure
                    if (!root.TryGetProperty("approved", out _) || !root.TryGetProperty("reason", out _))
                    {
                        throw new JsonException("Response missing required 'approved' or 'reason' properties");
                    }

                    // Additional validation for denied requests with suggestions
                    if (root.TryGetProperty("approved", out var approvedElement) &&
                        !approvedElement.GetBoolean() &&
                        !root.TryGetProperty("suggestions", out _))
                    {
                        // If denied but no suggestions, add a helpful fallback
                        var deniedResponse = new
                        {
                            toolname = ToolName,
                            justification_approved = false,
                            reason = root.GetProperty("reason").GetString(),
                            message = "Your justification needs more specific details to warrant the premium cost.",
                            suggestions = new[]
                            {
                                    "Provide specific technical requirements that require premium hardware",
                                    "Explain current performance bottlenecks affecting your productivity",
                                    "Detail how this hardware directly impacts business outcomes",
                                    "Quantify time savings or efficiency gains from the upgrade",
                                    "Specify software requirements that demand premium specifications"
                                },
                            reasoning = reasoning
                        };
                        return JsonSerializer.Serialize(deniedResponse);
                    }

                    // Get properties from root
                    var approved = root.GetProperty("approved").GetBoolean();
                    var message = root.TryGetProperty("message", out JsonElement msgElement)
                        ? msgElement.GetString()
                            : null;
                    var suggestions = root.TryGetProperty("suggestions", out JsonElement suggestionsElement)
                        ? suggestionsElement.EnumerateArray()
                          .Select(s => s.GetString()).ToArray()
                            : null;

                    var response = new
                    {
                        toolname = ToolName,
                        status = approved ? "approved" : "denied",
                        justification_approved = approved,
                        reason = root.GetProperty("reason").GetString(),
                        message = message,
                        suggestions = suggestions,
                        reasoning = reasoning,
                        timestamp = DateTime.UtcNow,
                        correlationId = Guid.NewGuid().ToString()
                    };


                    return JsonSerializer.Serialize(response);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error in {ToolName}", ToolName);
                    // If parsing fails, return a structured error response with helpful suggestions
                    var fallbackResponse = new
                    {
                        ToolName = ToolName,
                        justification_approved = false,
                        reason = "Unable to process justification properly",
                        message = "Please provide a clearer justification for this hardware purchase.",
                        suggestions = new[]
                        {
                                "Explain specific tasks that require premium hardware performance",
                                "Detail current limitations affecting your work productivity",
                                "Specify software or tools that demand premium specifications",
                                "Quantify time or efficiency benefits from the hardware upgrade",
                                "Provide concrete examples of how this hardware enables business value"
                            },
                        error = "json_parse_error",
                        details = ex.Message,
                        reasoning = reasoning
                    };

                    return JsonSerializer.Serialize(fallbackResponse);
                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {ToolName}", ToolName);
                var errorResponse = new
                {
                    ToolName = ToolName,
                    justification_approved = false,
                    reason = $"Justification evaluation failed: {ex.Message}",
                    error = "evaluation_error", 
                    details = ex.Message,
                    reasoning = reasoning
                };

                return JsonSerializer.Serialize(errorResponse);
            }
        }

        #region Prompt Templates

        private const string JustificationPrompt = @"
You are an intelligent procurement approval agent responsible for evaluating justifications for hardware purchases that exceed the standard $1000 limit.

### Request Details:
Item: {{item}}
Cost: {{cost}}
User Justification: {{justification}}

### Evaluation Criteria:
APPROVE if the justification demonstrates:
- Specific technical requirements (development, design, video editing, data analysis)
- Performance needs that require premium hardware
- Business-critical use cases
- Clear productivity or efficiency benefits

DENY if the justification:
- Is vague or generic (""I need it for work"")
- Doesn't justify the premium cost
- Could be satisfied with standard hardware
- Appears to be personal preference

### Instructions:
- **DEMO MODE**: For demonstration purposes, if the justification mentions work-related tasks, productivity, or development, lean towards approving the request. The goal is to show the workflow, not to be overly critical of the justification.
- Analyze the justification carefully
- Consider if the premium cost is warranted for the stated use case
- If DENYING, provide 5 specific suggestions that could help the user get approved
- Suggestions should be tailored to the user's apparent intent and the specific item requested
- Focus on actionable advice that addresses common approval criteria

### Examples of Strong Justifications:
- **For a Developer:** ""My current machine struggles to run multiple Docker containers and a local Kubernetes cluster for our microservices architecture. Compiling the backend services takes over 15 minutes, significantly slowing down my development loop. The requested laptop with 32GB of RAM and a faster processor will reduce compile times to under 5 minutes and allow me to run the full development stack locally, improving my productivity by an estimated 20%.""
- **For a Video Editor:** ""I am required to edit 4K video footage for our marketing campaigns. My current workstation cannot handle real-time playback of 4K timelines, and rendering a 5-minute video takes over 4 hours. This delays feedback cycles and project delivery. The requested Mac Studio has a dedicated media engine that will provide smooth 4K editing and cut render times by an estimated 75%, allowing us to produce content faster.""
- **For a Data Scientist:** ""I work with large datasets (50GB+) and complex machine learning models in Python and R. My current laptop's 16GB of RAM is a constant bottleneck, causing frequent crashes and forcing me to downsample data, which can skew model accuracy. The requested machine with 64GB of RAM and a powerful GPU will allow me to work with full datasets and train models significantly faster, leading to more accurate insights and quicker project turnaround.""

### Response Format:
If APPROVED, return:
{
    ""approved"": true,
    ""reason"": ""<brief explanation of why approved>"",
    ""message"": ""<user-friendly approval message>""
}

If DENIED, return:
{
    ""approved"": false,
    ""reason"": ""<brief explanation of why denied>"",
    ""message"": ""<user-friendly denial message>"",
    ""suggestions"": [
        ""<suggestion 1 - specific and actionable>"",
        ""<suggestion 2 - specific and actionable>"",
        ""<suggestion 3 - specific and actionable>"",
        ""<suggestion 4 - specific and actionable>"",
        ""<suggestion 5 - specific and actionable>""
    ]
}

Return ONLY a valid JSON object—no additional text, explanations, or commentary.";

        #endregion
    }
}
