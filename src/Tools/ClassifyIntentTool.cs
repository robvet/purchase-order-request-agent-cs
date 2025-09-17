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



    [Description("Classifies user requests for product procurement into one of: RequestProduct (order request), ShowAvailableProducts (list options), ShowProductSpecs (technical details), ShowComplianceRules (policy info), or IrrelevantInput (off-topic). Returns JSON with intent and confidence score (0.0-1.0).")]
    public class ClassifyIntentTool
    {
        private const string ToolName = "ClassifyIntentTool";
        private readonly ILogger<ClassifyIntentTool> _logger; // Logger for this agent
        private const string TracePrefix = "*** CUSTOM:"; // add prefix to custom trace messages for easy identification

        public ClassifyIntentTool(ILogger<ClassifyIntentTool> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), $"Thrown in {GetType().Name}");
        }

        [KernelFunction]
        [Description("Classifies user requests for product procurement into one of: RequestProduct (order request), ShowAvailableProducts (list options), ShowProductSpecs (technical details), ShowComplianceRules (policy info), or IrrelevantInput (off-topic). Returns JSON with intent and confidence score (0.0-1.0).")]
        public async Task<string> DetermineIntentAsync(
            Kernel kernel,

            [Description("Analyzes user input to classify product-related requests. Returns JSON with intent (RequestProduct, ShowAvailableProducts, ShowProductSpecs, ShowComplianceRules, or IrrelevantInput) and confidence score (0.0-1.0). Use when needing to determine the user's primary goal in a procurement context.")]
            string userPromptInput,

            [Description("(REQUIRED) The orchestrating model's explanation for selecting the intent classifier, including: 1) What triggered this classification need, 2) Why classification is needed at this step, 3) How it advances the conversation. Used for auditing and improving tool selection decisions.")]
            string reasoning)
        {
            try
            {
                _logger.LogInformation("{TracePrefix} Processing user request in {ToolName}: user prompt {UserPrompt}", TracePrefix, ToolName, userPromptInput);
                _logger.LogInformation("{TracePrefix} Processing user request in {ToolName} with reasoning: {Reasoning}", TracePrefix, ToolName, reasoning);

                // Validate parameters
                 if (string.IsNullOrWhiteSpace(userPromptInput))
                    throw new ArgumentException("User prompt required in {ToolName}.", ToolName);

                if (string.IsNullOrWhiteSpace(reasoning))
                    throw new ArgumentException("Reasoning required in {ToolName} for audit trail and tool selection improvement.", ToolName);

                // Prepare the prompt by replacing the variable
                string prompt = PromptTemplate.IntentRouterPrompt(userPromptInput)
                    .Replace("{{userPromptInput}}", userPromptInput);

                // Call the model using the kernel
                var result = await kernel.InvokePromptAsync(
                    prompt,
                    new KernelArguments
                    {
                        ["userPrompt"] = prompt
                    }
                );

                _logger.LogInformation("{TracePrefix} Model classification result in {ToolName}: {Output}", TracePrefix, ToolName, result.ToString());

                // Parse model response
                var json = JsonNode.Parse(result.ToString())
                    ?? throw new JsonException("Model returned null or invalid JSON response");

                var intent = json["intent"]?.ToString()
                    ?? throw new JsonException("Intent field missing from model response");

                var confidence = json["confidence"]?.GetValue<double>()
                    ?? throw new JsonException("Confidence score missing from model response");



                // After model call, before constructing response:
                bool needsRetry = false;
                string retryReason = "";

                // Validate intent is one of our known values
                var validIntents = new[] {
                    "RequestProduct",
                    "ShowAvailableProducts",
                    "ShowProductSpecs",
                    "ShowComplianceRules",
                    "IrrelevantInput"
                };

                if (!validIntents.Contains(intent))
                {
                    needsRetry = true;
                    retryReason += "Intent must be one of the specified values. ";
                }

                // Validate confidence meets our requirements
                if (confidence < 0.0 || confidence > 1.0)
                {
                    needsRetry = true;
                    retryReason += "Confidence must be between 0.0 and 1.0. ";
                }
                else if (intent == "IrrelevantInput" && confidence != 0.0)
                {
                    needsRetry = true;
                    retryReason += "IrrelevantInput must have confidence 0.0. ";
                }
                else if (intent != "IrrelevantInput" && confidence < 0.8)
                {
                    needsRetry = true;
                    retryReason += "Non-irrelevant intents must have confidence >= 0.8. ";
                }

                if (needsRetry)
                {
                    _logger.LogWarning("{TracePrefix} Intent/Confidence validation failed in {ToolName}. Intent: {Intent}, Confidence: {Confidence}. Retrying.", TracePrefix, ToolName, intent, confidence);

                    string retryPrompt = prompt + $@"

IMPORTANT - Your previous response needs correction:
{retryReason}

Reconsider the input carefully and provide a classification that meets ALL requirements:
1. Intent MUST be one of: {string.Join(", ", validIntents)}
2. Confidence MUST be between 0.0 and 1.0
3. IrrelevantInput MUST have confidence = 0.0
4. All other intents MUST have confidence >= 0.8

Previous attempt:
Intent: {intent}
Confidence: {confidence}";

                    // Retry classification
                    var retryResult = await kernel.InvokePromptAsync(
                        retryPrompt,
                        new KernelArguments { ["userPrompt"] = retryPrompt }
                    );

                    // Parse and validate retry response
                    var retryJson = JsonNode.Parse(retryResult.ToString())
                        ?? throw new JsonException("Retry returned null or invalid JSON");

                    intent = retryJson["intent"]?.ToString()
                        ?? throw new JsonException("Intent missing from retry response");

                    confidence = retryJson["confidence"]?.GetValue<double>()
                        ?? throw new JsonException("Confidence missing from retry response");

                    // If still invalid after retry, return needs_clarification
                    if (!validIntents.Contains(intent) ||
                        confidence < 0.0 || confidence > 1.0 ||
                        (intent == "IrrelevantInput" && confidence != 0.0) ||
                        (intent != "IrrelevantInput" && confidence < 0.8))
                    {
                        return JsonSerializer.Serialize(new
                        {
                            toolname= ToolName,
                            status = "needs_clarification",
                            message = "Unable to confidently classify your request. Please provide more specific details about what you need.",
                            original_intent = intent,
                            original_confidence = confidence,
                            reasoning = reasoning,
                            timestamp = DateTime.UtcNow,
                            correlationId = Guid.NewGuid().ToString()
                        });
                    }
                }

                // Construct enriched response with audit trail
                var response = new
                {
                    toolname = ToolName,
                    status = "completed",
                    intent = intent,
                    confidence = confidence, 
                    reasoning = reasoning,          // Include model's reasoning
                    timestamp = DateTime.UtcNow,
                };

                return JsonSerializer.Serialize(response);
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "{TracePrefix} Failed to parse model response in {ToolName}", TracePrefix, ToolName);
                var error = new
                {
                    toolname = ToolName,
                    status = "rerun",
                    error = "Invalid model response format",
                    details = jex.Message,
                    reasoning = reasoning,
                    timestamp = DateTime.UtcNow
                };
                return JsonSerializer.Serialize(error);
            }
            catch (ArgumentException aex)
            {
                _logger.LogError(aex, "{TracePrefix} Invalid argument provided in {ToolName}", TracePrefix, ToolName);
                var error = new
                {
                    toolname = ToolName,
                    error = "Invalid input parameter",
                    details = aex.Message,
                    reasoning = reasoning,
                    timestamp = DateTime.UtcNow
                };
                return JsonSerializer.Serialize(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{TracePrefix} Unexpected error in intent classification in {ToolName}", TracePrefix, ToolName);
                var error = new
                {
                    toolname = ToolName,
                    error = "Intent classification failed",
                    details = ex.Message,
                    reasoning = reasoning,
                    timestamp = DateTime.UtcNow
                };
                return JsonSerializer.Serialize(error);
            }
        }

        private static class PromptTemplate
        {
            public static string IntentRouterPrompt(string userPromptInput)
            {
                return @"You are a highly specialized AI assistant for a corporate purchasing system. 
Your only task is to analyze the user's input and classify their primary intent.

User input: {{userPromptInput}}

### Strict Classification Rules

1. MUST return ONE of these intents:
   - RequestProduct: User wants to order/request a product
   - ShowAvailableProducts: User wants to see product options
   - ShowProductSpecs: User wants technical details/specifications
   - ShowComplianceRules: User asks about policies
   - IrrelevantInput: Off-topic request

2. Confidence Score Requirements:
   - For IrrelevantInput: MUST be exactly 0.0
   - For all other intents: MUST be >= 0.8
   - All scores MUST be between 0.0 and 1.0

### JSON Output

**User Input**: ""Tell me about football scores""
Return STRICTLY valid JSON with the following structure:
{
  ""intent"": ""IrrelevantInput"",
  ""confidence"": 0.0
}

### Examples

**User Input**: ""I need to order a new laptop for a new hire""
**JSON Output**:
{
  ""intent"": ""RequestProduct"",
  ""confidence"": 0.98
}

---

**User Input**: ""Can I upgrade memory for the MBP-16-M3?""
**JSON Output**:
{
  ""intent"": ""ShowProductSpecs"",
  ""confidence"": 0.99
}

---

**User Input**: ""Show me the products that are available""
**JSON Output**:
{
  ""intent"": ""ShowAvailableProducts"",
  ""confidence"": 0.95
}";
            }
        }
    }
}






