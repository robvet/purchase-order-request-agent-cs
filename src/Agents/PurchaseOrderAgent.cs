using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using NearbyCS_API.Contracts;
using NearbyCS_API.Models;
// State store interface
using NearbyCS_API.Storage.Contract;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NearbyCS_API.Agents // Namespace for agent classes
{
    // Define the IInvoiceAgent interface if it does not exist elsewhere
    public class PurchaseOrderAgent : IPurchaseOrderAgent// Main agent class
    {
        private readonly Kernel _kernel; // Semantic Kernel instance for AI operations
        private readonly ILogger<PurchaseOrderAgent> _logger; // Logger for this agent
        private readonly IStateStore _stateStore; // Stores per-session/user state
        private readonly JsonSerializerOptions _jsonOptions;

        // Constructor with dependencies injected
        public PurchaseOrderAgent(ILogger<PurchaseOrderAgent> logger,
                           Kernel kernel,
                           IStateStore stateStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), $"Thrown in {GetType().Name}"); // Ensure logger not null
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel), $"Thrown in {GetType().Name}"); // Ensure kernel not null
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore), $"Thrown in {GetType().Name}"); // Ensure state store not null
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<(string completion, ChatHistory History)> ProcessUserRequestAsync(
            string userInputPrompt,
            string sessionId,
            TelemetryCollector telemetryCollector)
        {
            try
            {
                _logger.LogInformation("Processing user request: {UserPrompt}", userInputPrompt); // Log the user prompt

                // Fetch chat history for the session or create a new one
                var chatHistory = await _stateStore.GetChatHistoryAsync(sessionId) ?? new ChatHistory();

                // Add system prompt as the first message if history is empty
                if (chatHistory.Count == 0)
                {
                    chatHistory.AddSystemMessage(PromptTemplate.SystemPrompt());
                }

                userInputPrompt = PromptTemplate.UserPrompt().Replace("{{userInputPrompt}}", userInputPrompt);

                // Add the user's message to the chat history
                chatHistory.AddUserMessage(userInputPrompt);

                // Get the chat completion service from the kernel
                var chatService = _kernel.GetRequiredService<IChatCompletionService>();

                // Set up execution settings to auto-invoke kernel functions
                var settings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = 0.0 // Lower temperature for more deterministic responses
                };

                // Get the AI's response to the chat history
                var result = await chatService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings: settings,
                    kernel: _kernel);

                string completion = result.Content ?? ""; // Get the completion text

                // Track the last 'products' node if present in tool results
                JsonNode? lastProductsNode = null;
                try
                {
                    var json = JsonNode.Parse(completion);
                    if (json is JsonObject obj && obj.ContainsKey("products"))
                    {
                        lastProductsNode = obj["products"];
                    }
                }
                catch { /* Ignore parse errors here, handled elsewhere */ }

                // Log the completion
                telemetryCollector.Add($"[AGENT_RESPONSE] {completion}");

                // Add the assistant's response to the chat history
                chatHistory.AddAssistantMessage(completion);

                // Save the updated chat history for the session
                await _stateStore.SaveChatHistoryAsync(sessionId, chatHistory);

                // If multiple tools in a turn, only the last 'products' node is kept (already handled above)
                // You can pass lastProductsNode to the controller or include it in the completion as needed

                return (completion, chatHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Class}: {ErrorMessage}", GetType().Name, ex.Message);
                return ("An unexpected error occurred while processing your request: " + ex.Message, new ChatHistory());
            }
        }

        private static class PromptTemplate
        {
            public static string SystemPrompt()
            {
                return @"You are an autonomous invoice processing agent.

You are a goal-driven procurement agent responsible for managing employee purchase order requests from start to finish.

You are equipped with a set of intelligent tools. Use them selectively and in a thoughtful order. Reflect after each step and adjust your approach based on prior results.

Your goal is to ensure that every request:
- Is clearly understood
- Is available for processing
- Falls within budget
- Aligns with procurement policies
- Leverages existing inventory and vendor agreements
- Is fully structured and approved before submission

You may use the following tools:

1. ClassifyRequestTool – Identify the category or type of need (e.g., equipment, software, travel)
2. CheckPolicyComplianceToll – Review the request against all applicable procurement policies
3. SuggestAlternativesTool – Recommend lower-cost or faster-available options if appropriate
4. CheckInventoryOrTransfer – Determine if existing assets can satisfy the request

Use tools one at a time. Only proceed when the previous result is valid and compliant.

At every step:
- Reflect on tool output
- Capture any policy violations
- Adjust the plan as needed
- Stop or escalate if the request is invalid or non-compliant

Your reasoning, tool use, and outputs must demonstrate intelligent behavior and strong alignment with business policy.

Each tool call will return a structured result. Reflect on the result before deciding the next tool to use.

Do not guess or fabricate results. Stop if a step fails or requires human approval.

You must reason step-by-step and decide the best next action based on current memory and the goal.
";
            }

            public static string UserPrompt()
            {
                return @"
A new purchase order request has been submitted.

Request Details:
{{userInputPrompt}}

Your task is to process this request using the available tools. 
At each step, select and invoke the tool most appropriate for the current context, and reflect on the output before proceeding. 
Continue until the purchase order is ready for submission, or stop if the request is invalid, non-compliant, or requires escalation.

At the end of each interaction, respond ONLY with a valid JSON object containing these fields:

{
  ""reflection"": ""(Briefly explain your reasoning or the result for this step.)"",
  ""nextStep"": ""(What should the agent or user do next? E.g., ask for clarification, proceed to approval, etc.)"",
  ""userPrompt"": ""(The exact question or instruction for the user. No extra text.)"",
  ""products"": (If the user must select from a list of products, or if showing available products is helpful, include a JSON array of product objects here. Otherwise, omit this property.)
}

Do NOT include any text outside the JSON object.
";
            }

        }
    }
}



//1.ClassifyRequest – Identify the category or type of need (e.g., equipment, software, travel)
////2. CheckBudget – Verify available funds for the request
////3. SuggestVendors – Recommend preferred vendors based on category and sourcing rules
////4. BuildRequisition – Construct the full requisition object using validated details
////5. SubmitForApproval – Route the requisition for required approval if thresholds are exceeded
//6. CheckPolicyCompliance – Review the request against all applicable procurement policies
//7. SuggestAlternatives – Recommend lower-cost or faster-available options if appropriate
//8. CheckInventoryOrTransfer – Determine if existing assets can satisfy the request