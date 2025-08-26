using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SingleAgent.Contracts;
using SingleAgent.Models;
// State store interface
using SingleAgent.Storage.Contract;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SingleAgent.Agents // Namespace for agent classes
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

                // Load existing purchase request state or create new one
                var requestState = ReconstructStateFromHistory(chatHistory);

                // Add system prompt as the first message if history is empty
                if (chatHistory.Count == 0)
                {
                    chatHistory.AddSystemMessage(PromptTemplate.SystemPrompt());
                }

                userInputPrompt = PromptTemplate.UserPrompt()
                    .Replace("{{userInputPrompt}}", userInputPrompt)
                    .Replace("{{workflowState}}", JsonSerializer.Serialize(requestState, _jsonOptions));

                // Add the user's message to the chat history
                chatHistory.AddUserMessage(userInputPrompt);

                // Get the chat completion service from the kernel
                var chatService = _kernel.GetRequiredService<IChatCompletionService>();

                // Set up execution settings to auto-invoke kernel functions
                var settings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = 0.0
                };

                // Get the AI's response to the chat history
                var result = await chatService.GetChatMessageContentAsync(
                    chatHistory,
                    executionSettings: settings,
                    kernel: _kernel);

                string completion = result.Content ?? ""; // Get the completion text

                // Add the assistant's response to the chat history
                chatHistory.AddAssistantMessage(completion);

                // Save the updated chat history for the session
                await _stateStore.SaveChatHistoryAsync(sessionId, chatHistory);

                return (completion, chatHistory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Class}: {ErrorMessage}", GetType().Name, ex.Message);
                return ("An unexpected error occurred while processing your request: " + ex.Message, new ChatHistory());
            }
        }

        private PurchaseRequestState ReconstructStateFromHistory(ChatHistory chatHistory)
        {
            var state = new PurchaseRequestState
            {
                AdditionalData = new Dictionary<string, object>()
            };

            var toolMessages = chatHistory.Where(m => m.Role == AuthorRole.Tool).ToList();

            foreach (var toolMessage in toolMessages)
            {
                if (string.IsNullOrEmpty(toolMessage.Content)) continue;

                try
                {
                    var toolResult = JsonNode.Parse(toolMessage.Content);
                    if (toolResult == null) continue;

                    // Generic data points
                    if (toolResult["intent"] != null)
                    {
                        state.Intent = toolResult["intent"]?.ToString();
                        state.AdditionalData["lastCompletedTool"] = "ClassifyIntentTool";
                        state.Status = "classified";
                    }
                    if (toolResult["is_workplace_computer"]?.GetValue<bool>() == true)
                    {
                        state.AdditionalData["lastCompletedTool"] = "ValidateProductTool";
                        state.Status = "validated";
                    }
                    if (toolResult["sku"] != null && toolResult["sku"] is JsonArray skuArray)
                    {
                        state.MatchedSkus = skuArray.Select(s => s?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList()!;
                        state.AdditionalData["lastCompletedTool"] = "ExtractDetailsTool";
                        state.Status = "extracted";
                    }
                    if (toolResult["quantity"] != null)
                    {
                        state.Quantity = toolResult["quantity"]?.GetValue<int>();
                    }
                    if (toolResult["department"] != null)
                    {
                        state.Department = toolResult["department"]?.ToString();
                    }
                    if (toolResult["compliant"] != null)
                    {
                        state.AdditionalData["lastCompletedTool"] = "CheckComplianceTool";
                        state.Status = toolResult["compliant"]?.GetValue<bool>() == true ? "compliant" : "awaiting_justification";
                    }
                    if (toolResult["justification_approved"] != null)
                    {
                        state.AdditionalData["lastCompletedTool"] = "JustifyApprovalTool";
                        state.Status = toolResult["justification_approved"]?.GetValue<bool>() == true ? "justification_approved" : "justification_rejected";
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse tool result from chat history: {Content}", toolMessage.Content);
                }
            }

            _logger.LogInformation("Reconstructed State: Status={Status}, LastTool={LastTool}",
                state.Status,
                state.AdditionalData.TryGetValue("lastCompletedTool", out var tool) ? tool : "none");

            return state;
        }

        /// <architecture = "System Prompt Highlights" >
        ///   •	Dedicated ## Workflow Rules Section: This is the most important change. 
        ///     It gathers all the specific, non-negotiable instructions into one place. 
        ///     Makes prompt clearer and more structured
        ///     Significantly increases likelihood the model will follow the rules correctly
        ///   •	Consolidated Persona and Goal: Single, concise paragraph that clearly defines the agent's role
        ///   •	Clear Tool List: Fixes tool numbering and descriptions are slightly crisper and more action-oriented.
        ///   •	Structured Core Principles: Contains general "advice" or "best practices" for the agent, 
        ///     separating them from the hard rules. This distinction is important for the model's reasoning
        /// </architecture>

        private static class PromptTemplate
        {
            public static string SystemPrompt()
            {
                return @"You are a goal-driven, autonomous procurement agent. 
Your primary purpose is to manage employee purchase order requests from start to finish  by making intelligent, sequential use of the tools provided.

Tools

You may use the following tools:

  1. ClassifyIntentTool – Classifies an employee's request into a specific category: Request product, Show supported products, show product specs, show procurement policies.
  2. ValidateProductTool - Acts as a gatekeeper for the 'Request product' workflow to confirm the requested item is a workplace computer.
  3. ExtractDetailsTool – Extracts specific details like model, quantity, SKUs from a validated purchase request.
  4. CheckComplianceTool – Review the request against all applicable procurement policies.
  5. JustifyApprovalTool – Evaluates the justification for hardware purchases that violate compliance rules.

Core Principles:

  •	Reflect and Plan: After each tool use, reflect on the result and adjust your plan to achieve the goal.
  •	Reason Step-by-Step: Your internal monologue must show your reasoning for choosing each next action.
  •	Do Not Guess: If information is missing or a step fails, use your tools to get the information or stop and ask for human approval.
  •	Expect Structured JSON: All tools will return their results in a structured JSON format. Your next action 
    must be based on the key-value data contained within this JSON output.
  
Workflow Rules:

  •	Confidence Score Check: If the ClassifyIntentTool returns a confidence score below 0.8, you must stop all other actions. Immediately ask the user for clarification about their request.
  •	Purchase Request Validation: If the ClassifyIntentTool identifies the intent as 'RequestPurchase', the ONLY AVAILABLE tool for your next step is ValidateProductTool. You are forbidden from using any other tool, including ExtractDetailsTool, until ValidateProductTool has been successfully executed.
  •	Policy Tool Usage: The CheckComplianceTool can and should be used even if some request information is incomplete. It will determine which policies are applicable based on the available data.
  •	Justification Requirement: If the CheckComplianceTool returns 'compliant: false', your ONLY next step is to use the JustifyApprovalTool. You must ask the user for a justification first.

Workflow State Awareness:

• If you have access to previous workflow state, use it to continue where you left off
• Do not repeat tools that have already completed successfully  
• When asking for clarification, include context from previous steps
• Example: I found MacBook Pro options earlier. Which size do you prefer: 14 inch or 16 inch?
";
            }

            public static string UserPrompt()
            {
                return @"
Previous Workflow State (if any):
{{workflowState}}

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

        // Captures the chat history in a formatted string for debugging or logging
        private static string FormatChatHistory(ChatHistory chatHistory, JsonSerializerOptions _jsonOptions)
        {
            return string.Join(
                Environment.NewLine + new string('-', 40) + Environment.NewLine,
                chatHistory.Select(msg =>
                    JsonSerializer.Serialize(new
                    {
                        Role = msg.Role.ToString(),
                        Content = msg.Content
                    }, _jsonOptions)
                )
            );
        }

    }

}






//You are equipped with a set of intelligent tools. Use them selectively and in a thoughtful order. Reflect after each step and adjust your approach based on prior results.

//Your goal is to ensure that every request:
//-Is clearly understood
//- Is available for processing
//- Falls within budget
//- Aligns with procurement policies
//- Leverages existing inventory and vendor agreements
//- Is fully structured and approved before submission




//1.ClassifyRequest – Identify the category or type of need (e.g., equipment, software, travel)
////2. CheckBudget – Verify available funds for the request
////3. SuggestVendors – Recommend preferred vendors based on category and sourcing rules
////4. BuildRequisition – Construct the full requisition object using validated details
////5. SubmitForApproval – Route the requisition for required approval if thresholds are exceeded
//6. CheckPolicyCompliance – Review the request against all applicable procurement policies
//7. SuggestAlternatives – Recommend lower-cost or faster-available options if appropriate
//8. CheckInventoryOrTransfer – Determine if existing assets can satisfy the request



////// TODO: DEMO MODE - Debug inspection point for chat history state
////// ?? BREAKPOINT HERE: Inspect chatHistory and completion for debugging
////var debugChatState = new
////{
////    SessionId = sessionId,
////    MessageCount = chatHistory.Count,
////    LastCompletion = completion,
////    LastReflection = lastReflection, // NEW: Add the extracted reflection
////    ChatMessages = chatHistory.Select((msg, index) => new
////    {
////        Index = index,
////        Role = msg.Role.ToString(),
////        Content = msg.Content?.Substring(0, Math.Min(msg.Content.Length, 200)) + (msg.Content?.Length > 200 ? "..." : "")
////    }).ToList(),
////    FullChatHistoryJson = JsonSerializer.Serialize(chatHistory.Select(msg => new
////    {
////        Role = msg.Role.ToString(),
////        Content = msg.Content
////    }), _jsonOptions)
////};

//// Manual formatting for easy reading
//var debugOutput = new System.Text.StringBuilder();
//debugOutput.AppendLine($"SessionId: {debugChatState.SessionId}");
//debugOutput.AppendLine($"MessageCount: {debugChatState.MessageCount}");
//debugOutput.AppendLine($"LastCompletion: {debugChatState.LastCompletion}");
//debugOutput.AppendLine($"LastReflection: {debugChatState.LastReflection}");
//debugOutput.AppendLine("ChatMessages:");
//foreach (var msg in debugChatState.ChatMessages)
//{
//    debugOutput.AppendLine($"  Index: {msg.Index}");
//    debugOutput.AppendLine($"  Role: {msg.Role}");
//    debugOutput.AppendLine($"  Content: {msg.Content}");
//    debugOutput.AppendLine(new string('-', 30));
//}
//debugOutput.AppendLine("FullChatHistoryJson:");
//debugOutput.AppendLine(debugChatState.FullChatHistoryJson);


// If multiple tools in a turn, only the last 'products' node is kept (already handled above)
// You can pass lastProductsNode to the controller or include it in the completion as needed