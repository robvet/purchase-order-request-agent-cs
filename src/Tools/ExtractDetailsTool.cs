using Microsoft.SemanticKernel;
using SingleAgent.Models.DTO;
using SingleAgent.Storage.Contract;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SingleAgent.Tools
{
    [Description("Extracts structured order details—including model, quantity, department, confidence, warnings, errors, and status—from a user's purchase request. Returns a JSON object matching the extraction schema.")]
    public class ExtractDetailsTool
    {
        public string Name => "ExtractOrderDetailsTool";
        private readonly ILogger<ExtractDetailsTool> _logger; // Logger for this agent
        private readonly IProductRepository _productRepository;

        public ExtractDetailsTool(ILogger<ExtractDetailsTool> logger, IProductRepository productRepository)
        {
            _logger = logger;
            _productRepository = productRepository;
        }

        [KernelFunction]
        [Description("The user's purchase request in natural language, e.g., 'I need two Dell Latitude 5440s for QA.")]
        public async Task<string> ExtractDetailsAsync(
            Kernel kernel,
            [Description("Natural language text describing what the user wants to purchase.")] string userRequest,
            [Description("The user intent. This tool should only be used for 'RequestPurchase' intents.")] string intent)
        {
            try
            {
                _logger.LogInformation("Processing user request in ExtractDetailsTool: {userRequest}", userRequest);

                if (intent != "RequestPurchase")
                {
                    _logger.LogWarning("ExtractDetailsTool called with non-purchase intent: {Intent}", intent);

                    var errorResponse = new
                    {
                        status = "error",
                        error = "wrong_tool",
                        message = $"This tool extracts order details for purchase requests only. The current intent is '{intent}'.",
                        suggestion = "Use a tool appropriate for the current intent."
                    };
                    return JsonSerializer.Serialize(errorResponse);
                }

                // This line simply retrieves the raw, unchanged prompt string.
                // At this point, the string literally contains the characters {{userRequest}}.
                // It has not been replaced yet.
                //var toolPrompt = PromptTemplate.ExtractDetailsPrompt(userRequest);
                //var toolPrompt = PromptTemplate.ExtractDetailsPrompt();

                // In this line, we replace the {{userRequest}} placeholder with the actual user request.
                //var result = await kernel.InvokePromptAsync(toolPrompt, new KernelArguments { { "input", userRequest } });

                string prompt = PromptTemplate.ExtractDetailsPrompt(userRequest).Replace("{{userRequest}}", userRequest);

                // Call the model using the kernel
                var result = await kernel.InvokePromptAsync(
                    prompt,
                    new KernelArguments
                    {
                        ["userPrompt"] = prompt
                    }
                );

                _logger.LogInformation("Output from ExtractDetailsTool: {Output}", result.ToString());

                string rawJson = result.ToString();
                var json = JsonNode.Parse(rawJson);

                var status = json?["status"]?.ToString();
                var quantity = json?["quantity"]?.GetValue<int>();
                var department = json?["department"]?.ToString();
                var confidence = json?["confidence"]?.GetValue<double>() ?? 0.0;
                var sku = json?["sku"]?.AsArray()?.Select(s => s?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new List<string>();

                List<ProductDTO> products = new List<ProductDTO>();

                if (sku.Any())
                {
                    products = await _productRepository.GetBySkus(sku);
                }
                else if (status == "not_found")
                {
                    products = await _productRepository.GetAllProductsSummaryViewAsync();
                }

                var response = new
                {
                    status,
                    quantity,
                    department,
                    confidence,
                    sku,
                    products
                };

                return JsonSerializer.Serialize(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExtractDetailsTool");
                var error = new { error = $"Failed to process request: {ex.Message}" };
                return JsonSerializer.Serialize(error);
            }
        }


        private static class PromptTemplate
        {
            //public static string ExtractDetailsPrompt(string requestText)
            public static string ExtractDetailsPrompt(string userRequest)
            {
                return @"Extract order details from the user's purchase request.

    Supported products (sku: name):
    - MBP-16-M3: MacBook Pro 16"" (M3 Pro)
    - MBP-14-M3: MacBook Pro 14"" (M3 Pro)
    - DELL-LAT5440: Dell Latitude 5440
    - DELL-XPS13: Dell XPS 13
    - LEN-T14S: Lenovo ThinkPad T14s
    - LEN-X1C10: Lenovo ThinkPad X1 Carbon G10
    - HP-ELITE840: HP EliteBook 840 G10
    - SURF-LAP-STUDIO2: Surface Laptop Studio 2
    - SURF-PRO9: Surface Pro 9 Tablet
    - ASUS-EXPERT: ASUS ExpertBook B9
    - ACER-TMP6: Acer TravelMate P6

    User request: {{userRequest}}

    Identify the requested product(s) AND extract order details.

    Return STRICTLY valid JSON with these fields:
    {
      ""status"": ""matched"" | ""ambiguous"" | ""not_found"",
      ""sku"": [""array of matching SKUs only""],
      ""department"": ""extracted department name or null"",
      ""quantity"": number (default 1),
      ""confidence"": float between 0 and 1
    }

    Decision rules:
    - If the request matches exactly one product: status = ""matched""
    - If the request could refer to more than one product: status = ""ambiguous""
    - If no product is found: status = ""not_found""
    - Always return sku as an array, even for single matches

    Extraction rules:
    - Extract department ONLY if explicitly mentioned (e.g., ""for IT department"", ""engineering team needs"")
    - Extract quantity ONLY if explicitly mentioned (e.g., ""2 laptops"", ""three computers"")
    - If department is not mentioned: department = null
    - If quantity is not mentioned: quantity = 1

    Examples:
    Request: ""I need 2 MacBook Pros for the IT department""
    {""status"":""ambiguous"",""sku"":[""MBP-16-M3"",""MBP-14-M3""],""department"":""IT"",""quantity"":2,""confidence"":0.85}

    Request: ""Order a Dell XPS 13""
    {""status"":""matched"",""sku"":[""DELL-XPS13""],""department"":null,""quantity"":1,""confidence"":0.95}

    Request: ""Get me 5 ThinkPads""
    {""status"":""ambiguous"",""sku"":[""LEN-T14S"",""LEN-X1C10""],""department"":null,""quantity"":5,""confidence"":0.80}

    Request: ""I need a gaming laptop""
    {""status"":""not_found"",""sku"":[],""department"":null,""quantity"":1,""confidence"":0.90}

    Do NOT include any explanations, markdown, or extra text—return ONLY the JSON object.";
            }
        }
    }
}