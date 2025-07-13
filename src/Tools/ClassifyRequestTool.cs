using Microsoft.SemanticKernel;
using NearbyCS_API.Agents;
using System.ComponentModel;

[Description("Determines the category of a user's purchase request (e.g., hardware, software, services) using a language model.")]
public class ClassifyRequestTool
{
    public string Name => "ClassifyRequest";
    private readonly ILogger<PurchaseOrderAgent> _logger; // Logger for this agent

    public ClassifyRequestTool(ILogger<PurchaseOrderAgent> logger)
    {
        _logger = logger;
    }

    [KernelFunction]
    [Description("Returns the category and confidence score for a user’s purchase-related request in JSON format.")]
    public async Task<string> ClassifyRequestAsync(
        Kernel kernel,
        [Description("Natural language text describing what the user wants to purchase.")] string requestText)
    {
        _logger.LogInformation("Processing user request in ClassifyRequestTool: {UserPrompt}", requestText); // Log the user prompt

        // Prepare the prompt with the user request
        var toolPrompt = @"Classify the following user request into one of the following categories:
- Hardware
- Software
- Services
- Travel
- Furniture
- Other

You must respond ONLY with a valid JSON object in the following format. Do not include any additional text, explanations, or formatting:
{
  """"category"""": """"<category>"""",
  """"confidenceScore"""": <float between 0 and 1>
}

Example response:
{
  """"category"""": """"Hardware"""",
  """"confidenceScore"""": 0.95
}

User Request:
{{requestText}}
";

        toolPrompt = toolPrompt.Replace("{{requestText}}", requestText);

        //ClassifyRequestPrompts.ClassifyRequestPrompt.Replace("{{requestText}}", requestText);

        // Call the kernel to get the model's response
        var result = await kernel.InvokePromptAsync(toolPrompt, new() {
            { "requestText", requestText }
        });

        _logger.LogInformation("Output from ClassifyRequestTool: {Output}", result.ToString());

        // Return the model's raw response (should be JSON)
        return result.ToString();
    }
}