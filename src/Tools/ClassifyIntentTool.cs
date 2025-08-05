using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SingleAgent.Tools
{
    /// <architecture = "Single Responsibilty Principle" >
    ///   IntentClassificationTool is built to do one thing well: Classify the user's primary attempt 
    ///   from a know list. Validating or extraction in this step complicates logic and can easily 
    ///   degrade accuracy for the LM call. 
    ///   IntentRouterTool is responsible for determining the user's intent based on their input.
    ///   Entities are not needed yet in this step—they should be extracted later in specialized tools
    /// </architecture>
    /// 

    /// <architecture = "Intent vs. Entities" >
    ///   As emphasized in both NLU design and Semantic Kernel orchestration patterns, intent 
    ///   and entity extraction should be distinct steps—each handled by different components/tools.
    ///   IntentRouterTool is responsible for determining the user's intent based on their input.
    ///   Entities are not needed yet in this step—they should be extracted later in specialized tools
    /// </architecture>

    [Description("Analyzes a user's natural language input to classify their primary goal. It must return one of the following intents: RequestPurchase, ShowSupportedProducts, ShowSpecs, ShowPolicyComplianceSummary, or Help.")]
    public class ClassifyIntentTool
    {
        private readonly ILogger<ClassifyIntentTool> _logger; // Logger for this agent

        public ClassifyIntentTool(ILogger<ClassifyIntentTool> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), $"Thrown in {GetType().Name}");
        }

        [KernelFunction]
        [Description("Determines the primary intent and a confidence score for any user request made to the purchasing system.")]
        public async Task<string> DetermineIntentAsync(
            Kernel kernel,
            [Description("The initial, unprocessed text query from the user that needs to be classified.")] string userPromptInput)
        {
            try
            {
                _logger.LogInformation("Processing user request in IntentRouterTool: {UserPrompt}", userPromptInput); // Log the user prompt

                // Prepare the prompt by replacing the variable
                string prompt = PromptTemplate.IntentRouterPrompt(userPromptInput).Replace("{{userPromptInput}}", userPromptInput);

                // Call the model using the kernel
                var result = await kernel.InvokePromptAsync(
                    prompt,
                    new KernelArguments
                    {
                        ["userPrompt"] = prompt
                    }
                );

                _logger.LogInformation("Output from IntentRouterTool: {Output}", result.ToString());

                // Parse and enrich ambiguous response
                string rawJson = result.ToString();

                // Parse the model's response
                var json = JsonNode.Parse(rawJson);
                var intent = json?["intent"]?.ToString();
                var confidence = json?["confidence"]?.GetValue<double>() ?? 0.0;

                var response = new
                {
                    intent,
                    confidence
                };
                return JsonSerializer.Serialize(response);
            }
            catch (Exception ex)
            {
                // Return error response
                // Serialize the error as JSON string
                var error = new { error = $"Failed to parse model response: {ex.Message}" };
                return JsonSerializer.Serialize(error);
            }
        }

        private static class PromptTemplate
        {
            public static string IntentRouterPrompt(string userPromptInput)
            {
                return @"You are a highly specialized AI assistant for an corporate purchasing system. 
Your only task is to analyze the user's input and classify their primary intent.

User input: {{userPromptInput}}

### Intents
 - **RequestPurchase**: The user wants to buy or order a new item.
 - **ShowSupportedProducts**: The user is asking for a list of available products.
 - **ShowSpecs**: The user is asking for the technical specifications of a specific product.
 - **ShowComplianceRules**: The user is asking about the company's purchasing policy.
 - **Other**: Something that is not relevant for this AI application. Set Confidence to 0.0.

- confidence: A float value between 0.0 and 1.0 indicating how confident the model is in its classification.

### JSON Output
Return STRICTLY valid JSON with the following structure:
{
  ""intent"": ""One of the intents listed above"",
  ""confidence"": 0.0
}

### Examples

**User Input**: ""I need to order a new laptop for a new hire""
**JSON Output**:
{
  ""intent"": ""RequestPurchase"",
  ""confidence"": 0.98
}

---

**User Input**: ""What are the specs for the MBP-16-M3?""
**JSON Output**:
{
  ""intent"": ""ShowSpecs"",
  ""confidence"": 0.99
}

---

**User Input**: ""Show me the products that are available""
**JSON Output**:
{
  ""intent"": ""ShowSupportedProducts"",
  ""confidence"": 0.95
}";
            }
        }
    }
}






