using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SingleAgent.Tools
{
    /// <architecture = "Intent vs. Entities" >
    ///   As emphasized in both NLU design and Semantic Kernel orchestration patterns, intent 
    ///   and entity extraction should be distinct steps—each handled by different components/tools.
    ///   IntentRouterTool is responsible for determining the user's intent based on their input.
    ///   Entities are not needed yet in this step—they should be extracted later in specialized tools
    /// </architecture>

    [Description("Determines the category of a user's purchase request (e.g., hardware, software, services) using a language model.")]
    public class IntentRoutingTool
    {
        public string Name => "IntentRouterTool";
        private readonly ILogger<IntentRoutingTool> _logger; // Logger for this agent

        public IntentRoutingTool(ILogger<IntentRoutingTool> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), $"Thrown in {GetType().Name}");
        }

        [KernelFunction]
        [Description("Returns the category and confidence score for a user’s purchase-related request in JSON format.")]
        public async Task<string> DetermineIntentAsync(
            Kernel kernel,
            [Description("Natural language text describing what the user wants to purchase.")] string userPromptInput)
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
                var confidence= json?["confidence"]?.GetValue<double>() ?? 0.0;
                var userRequest = json?["userRequest"]?.ToString();
                //var errors = json?["errors"]?.ToString();

                var response = new
                {
                    intent,
                    confidence,
                    userRequest
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
                return @"You are an intent-and-entity extractor.
User input: {{userPromptInput}}

Return STRICTLY valid JSON:

- intent: one of [ RequestPurchase, ShowSupportedModels, ShowSpecs, ShowPolicySummary, Help ]
- confidence: float between 0 and 1
- userRequest: the user's request text
- errors: empty or list of strings

Example:
{
  ""intent"": ""ShowSpecs"",
  ""entities"": { ""model"": ""MBP-16-M3"" },
  ""confidence"": 0.92,
  ""userRequest"": ""What are the specs for the MBP-16-M3?"",
  ""errors"": []
}";
            }
        }

    }
}

//// Output format
//{
//    "intent": "ShowSpecs",
//  "entities": { "model": "MBP-16-M3" },
//  "confidence": 0.87,
//  "rawText": "What are the specs for the MBP‑16‑M3?",
//  "errors": [] // optionally report issues, like missing entity
//}





