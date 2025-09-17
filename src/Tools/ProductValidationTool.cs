using Microsoft.SemanticKernel;
using SingleAgent.Models.DTO;
using SingleAgent.Storage.Contract;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SingleAgent.Tools
{
    [Description("Validates requested products against supported catalog, handling exact matches, ambiguous matches, and non-matches. Returns JSON with status, SKUs, quantity, and confidence score.")]
    public class ProductValidationTool
    {
        private const string ToolName = "ProductValidationTool";
        private readonly ILogger<ProductValidationTool> _logger; // Logger for this agent
        private readonly IProductRepository _productRepository;
        private const string TracePrefix = "*** CUSTOM:"; // add prefix to custom trace messages for easy identification 

        public ProductValidationTool(ILogger<ProductValidationTool> logger, IProductRepository productRepository)
        {
            _logger = logger;
            _productRepository = productRepository;
        }

        [KernelFunction]
        [Description("The user's purchase request in natural language, e.g., 'I need two Dell Latitude 5440s for QA.")]
        public async Task<string> ValidateRequestedProductAsync(
            Kernel kernel,
            [Description("Natural language text describing the product the user wants to request.")] string userRequest,
            [Description("The user intent. This tool should only be used for 'RequestProduct' intents.")] string intent,
            [Description("(REQUIRED) Model's explanation for why product validation is needed at this step")] string reasoning)
        {
            try
            {
                _logger.LogInformation("{TracePrefix} Processing user request in {ToolName}: {userRequest}", TracePrefix, ToolName, userRequest);
                _logger.LogInformation("{TracePrefix} Processing user request in {ToolName}:intent {intent}", TracePrefix, ToolName, intent);
                _logger.LogInformation("{TracePrefix} Processing user request in {ToolName} with reasoning: {Reasoning}", TracePrefix, ToolName, reasoning);

                // Validate parameters
                if (string.IsNullOrWhiteSpace(userRequest))
                    throw new ArgumentException("User request required in {ToolName}.", ToolName);

                if (string.IsNullOrWhiteSpace(reasoning))
                    throw new ArgumentException("Reasoning required in {ToolName} for audit trail and tool selection improvement.", ToolName);

                if (intent != "RequestProduct")
                {
                    ///<ArchitectureNote = Self-Correction>
                    ///  If the user intent is not a purchase request, don't return an exception and abort the workflow based on 
                    ///  single incorrect LLM decision
                    ///  
                    ///  Instead, attempt to SELF CORRECT by returning a structured response that helps the LLM 
                    ///  understand it made an error and guide it to correct course. These types of behaviors are crucial for 
                    ///  production AI Agents.
                    ///  
                    /// The response:
                    ///    1. Returns a valid JSON response that the LLM can process (as opposed to crashing the workflow)
                    ///    2. Clearly indicates the error - "wrong_intent" 
                    ///    3. Provides context - Explains why this tool isn't appropriate
                    ///    4. Offers guidance - Suggests what the LLM should do next
                    ///    5. Maintains consistency - Returns JSON like all other responses
                    ///    
                    /// The LLM can then:
                    ///    1. Recognize the error
                    ///    2. Read the context
                    ///    3. Read the suggestion
                    ///    4. Reason about which tool to use next
                    ///    5. Continue the workflow without crashing or manual intervention
                    ///</ArchitectureNote>

                    // The LogWarning observability call is vital. It allows you, the developer, to track how often the LLM
                    // makes mistakes and in what contexts, which is invaluable for debugging and fine-tuning the agent's main prompts.
                    _logger.LogWarning("{TracePrefix} {ToolName} called with non-purchase intent: {Intent}", TracePrefix, ToolName, intent);

                    var errorResponse = new
                    {
                        ToolName = ToolName,
                        status = "rerun",
                        intent = intent,
                        error = "wrong_intent",
                        message = $"{ToolName} validates purchase requests only. The current intent is '{intent}'.",
                        suggestion = "Use a tool appropriate for the current intent.",
                        confidence = 0.0,
                        reasoning = reasoning
                    };

                    return JsonSerializer.Serialize(errorResponse);
                }

                // This line simply retrieves the raw, unchanged prompt string.
                // At this point, the string literally contains the characters {{userRequest}}.
                // It has not been replaced yet.
                //var toolPrompt = PromptTemplate.ValidateProduct(userRequest);
                //var toolPrompt = PromptTemplate.ValidateProduct();

                // In this line, we replace the {{userRequest}} placeholder with the actual user request.
                //var result = await kernel.InvokePromptAsync(toolPrompt, new KernelArguments { { "input", userRequest } });

                string prompt = PromptTemplate.ValidateProduct(userRequest).Replace("{{userRequest}}", userRequest);

                // Call the model using the kernel
                var result = await kernel.InvokePromptAsync(
                    prompt,
                    new KernelArguments
                    {
                        ["userPrompt"] = prompt
                    }
                );

                _logger.LogInformation("{TracePrefix} Output from {ToolName} : {Output}", TracePrefix, ToolName, result.ToString());

                string rawJson = result.ToString();
                var json = JsonNode.Parse(rawJson);

                var status = json?["status"]?.ToString();
                var quantity = json?["quantity"]?.GetValue<int>();
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
                    ToolName = ToolName,
                    status = "completed",
                    confidence = confidence,
                    quantity = quantity,
                    sku = sku,
                    products = products,
                    reasoning = reasoning,
                    timestamp = DateTime.UtcNow
                };

                return JsonSerializer.Serialize(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{TracePrefix}  Error in {ToolName}", TracePrefix, ToolName);
                return JsonSerializer.Serialize(new
                {
                    ToolName = ToolName,
                    status = "rerun",
                    error = "processing_error",
                    message = "Failed to process product validation details.",
                    details = ex.Message,
                    reasoning = reasoning,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        private static class PromptTemplate
        {
            //public static string ValidateProduct(string requestText)
            public static string ValidateProduct(string userRequest)
            {
                return @"Validate if the requested product exists in our supported catalog.

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

Return STRICTLY valid JSON with these fields:
{
    ""status"": ""matched"" | ""ambiguous"" | ""not_found"",
    ""sku"": [""array of matching SKUs only""],
    ""quantity"": number (default 1),
    ""confidence"": float between 0 and 1
}

Decision rules:
- If request matches exactly one product: status = ""matched""
- If request could refer to multiple products: status = ""ambiguous""
- If no product found: status = ""not_found""
- Always return sku as array, even for single matches
- If quantity not mentioned, default to 1

Examples:
Request: ""I need 2 MacBook Pros""
{""status"":""ambiguous"",""sku"":[""MBP-16-M3"",""MBP-14-M3""],""quantity"":2,""confidence"":0.95}

Request: ""Order a Dell XPS 13""
{""status"":""matched"",""sku"":[""DELL-XPS13""],""quantity"":1,""confidence"":0.98}

Request: ""Get me 5 ThinkPads""
{""status"":""ambiguous"",""sku"":[""LEN-T14S"",""LEN-X1C10""],""quantity"":5,""confidence"":0.90}

Request: ""I need a gaming laptop""
{""status"":""not_found"",""sku"":[],""quantity"":1,""confidence"":0.99}

Do NOT include any explanations, markdown, or extra text—return ONLY the JSON object.";
            }
        }
    }
}