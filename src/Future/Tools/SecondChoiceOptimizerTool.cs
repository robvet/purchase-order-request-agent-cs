using Microsoft.SemanticKernel;
using NearbyCS_API.Agents;
using NearbyCS_API.Models.DTO;
using NearbyCS_API.Storage.Contract;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NearbyCS_API.Future.Tools
{
    public class SecondChoiceOptimizerTool
    {
        private readonly ILogger<PurchaseOrderAgent> _logger; // Logger for this agent
        private readonly IProductRepository _productRepository;

        public string Name => "SuggestAlternativesTool";

        public SecondChoiceOptimizerTool(ILogger<PurchaseOrderAgent> logger, IProductRepository productRepository)
        {
            _logger = logger;
            _productRepository = productRepository;
        }

        [KernelFunction]
        [Description("Returns a list of suggested alternative products based on the current product inventory.")]
        public async Task<string> SuggestAlternativeAsync(
            Kernel kernel,
            [Description("The input text to trigger alternative suggestions.")] string requestedComputer,
            [Description("The input text to trigger alternative suggestions.")] string context)
        {
            _logger.LogInformation("Processing user request in SuggestAlternativesTool: {UserPrompt}", requestedComputer); // Log the user prompt

            var products = await _productRepository.GetAllProductsSummaryViewAsync();
            var productsJson = JsonSerializer.Serialize(products);

            var toolPrompt = PromptTemplate.SuggestAlternativesPromptTemplate(requestedComputer)
                .Replace("{{requestedLaptop}}", requestedComputer)
                .Replace("{{requestedComputer}}", productsJson)
                .Replace("{{context}}", context);

            // Call the kernel to get the model's response
            var result = await kernel.InvokePromptAsync(toolPrompt, new() {
                { "alternates", requestedComputer }
            });

            _logger.LogInformation("Output from SuggestAlternativeTool LLM Inference: {Output}", result.ToString());

            string rawJson = result.ToString();

            try
            {
                var json = JsonNode.Parse(rawJson);
                var sku = json?["sku"]?.ToString();
                var name = json?["name"]?.ToString();
                var reason = json?["reason"]?.AsArray()?.Select(s => s?.ToString()).ToList() ?? new List<string>();

                // Construct your API response object
                var response = new
                {
                    sku,
                    name,
                    reason
                };

                return JsonSerializer.Serialize(response); // Or however you write JSON in your API framework
            }
            catch (Exception ex)
            {
                // Return error response
                // Serialize the error as JSON string
                var error = new { error = $"Failed to parse model response: {ex.Message}" };
                return JsonSerializer.Serialize(error);
            }
        }

        // Change 'private static class PromptTemplate' to 'static class PromptTemplate'
        private static class PromptTemplate
        {
            public static string SuggestAlternativesPromptTemplate(string requestText)
            {
                return @"Given the user's requested laptop:
- Laptop Requested: {requestedLaptop}
- Relevant Context: {context}   // (e.g., ""User needs 32GB RAM"", ""Budget under $2200"", ""Prefers lightweight models"", etc.)

Here is a JSON array of available alternative computers and their specs:
{availableAlternativesJson}

Instructions:
1. Review the list of available computers.
2. Select the three best alternatives to the requested laptop, considering the context provided.
3. For each alternative, explain specifically why it is a strong alternative for this user (e.g., better performance, lower price, similar features, in stock, lighter weight, etc.).

Return your response as a JSON array:
[
  {
    ""sku"": ""<Alternative SKU>"",
    ""name"": ""<Alternative Name>"",
    ""reason"": ""<Why this is a strong alternative for the user's needs>""
  },  
]";
            }
        }
    }
}



