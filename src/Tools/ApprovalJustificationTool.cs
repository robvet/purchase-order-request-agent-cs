using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;

namespace SingleAgent.Tools
{
    public class ApprovalJustificationTool
    {
        public string Name => "ApprovalJustificationTool";

        [KernelFunction]
        [Description("Evaluates justification for hardware purchases that exceed the $1000 cost limit")]
        public async Task<string> EvaluateJustificationAsync(
            Kernel kernel,
            [Description("The justification provided by the user for exceeding the cost limit")] string justification,
            [Description("The requested hardware item that exceeds the limit")] string item,
            [Description("The cost that exceeds the $1000 limit")] decimal cost)
        {
            try
            {
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
                            approved = false,
                            reason = root.GetProperty("reason").GetString(),
                            message = "Your justification needs more specific details to warrant the premium cost.",
                            suggestions = new[]
                            {
                                "Provide specific technical requirements that require premium hardware",
                                "Explain current performance bottlenecks affecting your productivity",
                                "Detail how this hardware directly impacts business outcomes",
                                "Quantify time savings or efficiency gains from the upgrade",
                                "Specify software requirements that demand premium specifications"
                            }
                        };
                        return JsonSerializer.Serialize(deniedResponse);
                    }
                    
                    return rawJson;
                }
                catch (JsonException)
                {
                    // If parsing fails, return a structured error response with helpful suggestions
                    var fallbackResponse = new
                    {
                        approved = false,
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
                        error = "json_parse_error"
                    };
                    
                    return JsonSerializer.Serialize(fallbackResponse);
                }
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    approved = false,
                    reason = $"Justification evaluation failed: {ex.Message}",
                    error = "evaluation_error"
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
- Analyze the justification carefully
- Consider if the premium cost is warranted for the stated use case
- If DENYING, provide 5 specific suggestions that could help the user get approved
- Suggestions should be tailored to the user's apparent intent and the specific item requested
- Focus on actionable advice that addresses common approval criteria

### Response Format:
If APPROVED, return:
{
  ""approved"": true,
  ""reason"": ""<brief explanation of why approved>"",
  ""message"": ""<user-friendly approval message>"",
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
    ""<suggestion 5 - specific and actionable>"",
  ]
}

### Example Suggestions for Common Scenarios:

For development work:
- ""Specify which development tools require premium hardware (e.g., 'Android Studio compilation takes 45 minutes on current machine')""
- ""Explain specific performance bottlenecks affecting productivity (e.g., 'Current laptop crashes during Docker builds')""
 
For creative work:
- ""Detail file sizes and rendering times for your typical projects (e.g., '4K video projects take 8 hours to export')""
- ""Mention specific software requirements (e.g., 'Adobe Premiere requires 32GB RAM for smooth 4K editing')""
 
For analysis work:
- ""Quantify dataset sizes and processing times (e.g., 'Processing 10GB datasets takes 6 hours currently')"",
- ""Specify software requirements (e.g., 'Machine learning models require GPU acceleration')"",
 
Return ONLY a valid JSON object—no additional text, explanations, or commentary.
";

        #endregion
    }
}
