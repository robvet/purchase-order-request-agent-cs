using common.Enums;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SingleAgent.Contracts;
using SingleAgent.Models;
using SingleAgent.Models.DTO;
using SingleAgent.Prompting;

// State store interface
using SingleAgent.Storage.Contract;
using System.Text.Json;

namespace SingleAgent.Agents // Namespace for agent classes
{
    // Define the IInvoiceAgent interface if it does not exist elsewhere
    public class PurchaseOrderAgent : IPurchaseOrderAgent// Main agent class
    {
        private readonly Kernel _kernel; // Semantic Kernel instance for AI operations
        private readonly ILogger<PurchaseOrderAgent> _logger; // Logger for this agent
        private readonly IStateStore _stateStore; // Stores per-session/user state
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IConfiguration _configuration;
        private readonly int _temperature;
        private List<MessageThreadModel> messageThreads = new List<MessageThreadModel>();

        private const string TracePrefix = "*** CUSTOM:"; // add prefix to custom trace messages for easy identification 

        /// <architecture = "Workflow">
        /// 1) Agents and Tools should be contain orchestration and validation logic. 
        /// 2) The model should be used for reasoning, decision-making, and language tasks for the specific business case.
        /// 3) Single source of truth: ChatHistory
        /// 4) Track state naturally through chat history
        /// </architecture>

        // Constructor with dependencies injected
        public PurchaseOrderAgent(ILogger<PurchaseOrderAgent> logger,
                           Kernel kernel,
                           IStateStore stateStore,
                           IConfiguration configuration
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), $"Thrown in {GetType().Name}"); // Ensure logger not null
            _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel), $"Thrown in {GetType().Name}"); // Ensure kernel not null
            _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore), $"Thrown in {GetType().Name}"); // Ensure state store not null
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration), $"Thrown in {GetType().Name}"); // Ensure configuration not null

            // if temperature not set in config, default to 1
            _temperature = _configuration.GetValue<int?>("inference-temperature") ?? 1;

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<(string completion, ResponseInformationDto responseInformationDto)> ProcessUserRequestAsync(
            string userInput,
            string sessionId,
            TelemetryCollector telemetryCollector)
        {
            try
            {
                _logger.LogInformation("{TracePrefix} Processing user request: {UserPrompt}", TracePrefix,userInput); // Log the user prompt

                                
                // 1. Full History Store (for audit/tracking)
                var fullHistory = await _stateStore.GetChatHistoryAsync(sessionId) ?? new ChatHistory();
                                               
                // 2. Add system prompt if new conversation
                if (fullHistory.Count == 0)
                {
                    fullHistory.AddSystemMessage(PurchaseOrderPrompts.SystemPrompt());
                }

                // 3. Add current input to full history
                fullHistory.AddUserMessage(userInput);

                messageThreads.Add(new MessageThreadModel(Role.System, PurchaseOrderPrompts.SystemPrompt(), 0));
                messageThreads.Add(new MessageThreadModel(Role.User, userInput, 0));

                // 4. Let ContextPruningService build model context
                //var modelContext = _contextPruningService.BuildModelContext(fullHistory);

                //// 4. Build model context (only what's needed)  
                ////    Encapsulate in OpenAI ChatHistory object
                ////    
                //var modelContext = new ChatHistory();

                /// <architecture = "Workflow">
                /// 1) NO HARDCODING in orchestration code. Keep workflow state in the system prompt - let it guide the model.
                ///    Let model choose next steps based on context and tools available
                /// 2) Single source of truth: ChatHistory
                /// 3) Track state naturally through chat history
                /// </architecture>

                //// Always start fresh with system prompt
                //modelContext.AddSystemMessage(PurchaseOrderPrompts.SystemPrompt());

                //// Add only relevant history (last tool result + response)
                //var lastRelevantMessages = GetRelevantMessages(fullHistory);

                //foreach (var msg in lastRelevantMessages)
                //{
                //    modelContext.AddMessage(msg.Role, msg.Content);
                //}

                //// Add current user input
                //modelContext.AddUserMessage(userInput);

                // Get chat completion service from kernel and configure auto-invoke kernel functions
                var chatService = _kernel.GetRequiredService<IChatCompletionService>();
                var settings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = _temperature  // must be set to 1 from gpt-5
                };

                // Get model response using focused context
                var result = await chatService.GetChatMessageContentAsync(
                    fullHistory,  // Only what model needs
                    executionSettings: settings,
                    kernel: _kernel
                );

                // Get token count
                var innerContent = result.InnerContent as OpenAI.Chat.ChatCompletion;
                var inputTokens = (innerContent != null) ? innerContent.Usage.InputTokenCount : 0;
                var outputTokens = (innerContent != null) ? innerContent.Usage.OutputTokenCount : 0;
                var reasoningTokens = (innerContent != null) ? innerContent.Usage.OutputTokenDetails.ReasoningTokenCount : 0;

                var responseInformationDto = new ResponseInformationDto(inputTokens, outputTokens, reasoningTokens);

                ////if (result.Metadata.TryGetValue("Usage", out var usage) && usage is OpenAIUsage u)
                ////{
                ////    promptTokens = u.PromptTokens;
                ////    completionTokens = u.CompletionTokens;
                ////    totalTokens = u.TotalTokens;
                ////}

                //var response = result.Content[0];

                //int inputTokens = 0, outputTokens = 0, totalTokens = 0;

                //if (result.Metadata is { } meta &&
                //    meta.TryGetValue("Usage", out var usageObj) &&
                //    usageObj is ChatTokenUsage usage)
                //{
                //    inputTokens = usage.InputTokenCount;
                //    outputTokens = usage.OutputTokenCount;
                //    totalTokens = usage.TotalTokenCount;
                //}
                //else

                //messageThreads.Add(new MessageThreadModel(Role.Assistant, result.Content, TotalTokens));

                //_logger.LogInformation("Tokens in={In} out={Out} total={Total}", inputTokens, outputTokens, totalTokens);
                //_logger.LogInformation("Tokens prompt={P} completion={C} total={T}", prompt, completion, total);

                // Save assistant response and history
                fullHistory.AddAssistantMessage(result.Content);

                // Update fullchat history for the session
                await _stateStore.SaveChatHistoryAsync(sessionId, fullHistory);

                _logger.LogInformation("{TracePrefix} Model final response: {Response}", TracePrefix, result.Content);

                return (result.Content, responseInformationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{TracePrefix} Error in {Class}: {ErrorMessage}", TracePrefix, GetType().Name, ex.Message);
                throw;
            }
        }
    }
}



//static int GetInt(IReadOnlyDictionary<string, object?> m, params string[] keys)
//{
//    foreach (var k in keys)
//        if (m.TryGetValue(k, out var v) && int.TryParse(v?.ToString(), out var val)) return val;
//    return 0;
//}


//static int TryInt(IReadOnlyDictionary<string, object?> m, params string[] keys)
//{

//    foreach (var k in keys)
//        if (m.TryGetValue(k, out var v) && int.TryParse(v?.ToString(), out var val))
//            return val;
//    return 0;
//}


//private static (int prompt, int completion, int total) ExtractUsage(ChatMessageContent msg)
//{
//    if (msg?.Metadata == null) return (0, 0, 0);

//    // Direct OpenAIUsage object
//    if (msg.Metadata.TryGetValue("Usage", out var usageObj) && usageObj is OpenAIUsage u)
//    {
//        return (u.PromptTokens, u.CompletionTokens, u.TotalTokens);
//    }

//    // Fallback flattened keys
//    int prompt = TryInt(msg.Metadata, "prompt_tokens", "PromptTokens");
//    int completion = TryInt(msg.Metadata, "completion_tokens", "CompletionTokens");
//    int total = TryInt(msg.Metadata, "total_tokens", "TotalTokens");
//    if (total == 0) total = prompt + completion;
//    return (prompt, completion, total);

//    static int TryInt(IReadOnlyDictionary<string, object?> meta, params string[] keys)
//    {
//        foreach (var k in keys)
//        {
//            if (meta.TryGetValue(k, out var v) && int.TryParse(v?.ToString(), out var val))
//                return val;
//        }
//        return 0;
//    }
//}


//// Allows agent to maintain stateful behavior across conversation turns without needing a separate state database.
//// Uses chat history as the source of truth, reconstructing current purchase state at the start of each turn, and
//// inserts at the beginning of user prompt. Analyzes tool outputs in chat history to rebuild state.
//// Tracks workflow progression by setting the status and "lastCompletedTool" in AdditionalData.
//private PurchaseRequestState ReconstructStateFromHistory(ChatHistory chatHistory)
//{
//    var state = new PurchaseRequestState
//    {
//        AdditionalData = new Dictionary<string, object>()
//    };

//    var toolMessages = chatHistory.Where(m => m.Role == AuthorRole.Tool).ToList();

//    foreach (var toolMessage in toolMessages)
//    {
//        if (string.IsNullOrEmpty(toolMessage.Content)) continue;

//        try
//        {
//            var toolResult = JsonNode.Parse(toolMessage.Content);
//            if (toolResult == null) continue;

//            // Generic data points
//            if (toolResult["intent"] != null)
//            {
//                state.Intent = toolResult["intent"]?.ToString();
//                state.AdditionalData["lastCompletedTool"] = "ClassifyIntentTool";
//                state.Status = "classified";
//            }
//            if (toolResult["is_workplace_computer"]?.GetValue<bool>() == true)
//            {
//                state.AdditionalData["lastCompletedTool"] = "ValidateProductTool";
//                state.Status = "validated";
//            }
//            if (toolResult["sku"] != null && toolResult["sku"] is JsonArray skuArray)
//            {
//                state.MatchedSkus = skuArray.Select(s => s?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList()!;
//                state.AdditionalData["lastCompletedTool"] = "ExtractDetailsTool";
//                state.Status = "extracted";
//            }
//            if (toolResult["quantity"] != null)
//            {
//                state.Quantity = toolResult["quantity"]?.GetValue<int>();
//            }
//            if (toolResult["department"] != null)
//            {
//                state.Department = toolResult["department"]?.ToString();
//            }
//            if (toolResult["compliant"] != null)
//            {
//                state.AdditionalData["lastCompletedTool"] = "CheckComplianceTool";
//                state.Status = toolResult["compliant"]?.GetValue<bool>() == true ? "compliant" : "awaiting_justification";
//            }
//            if (toolResult["justification_approved"] != null)
//            {
//                state.AdditionalData["lastCompletedTool"] = "JustifyApprovalTool";
//                state.Status = toolResult["justification_approved"]?.GetValue<bool>() == true ? "justification_approved" : "justification_rejected";
//            }
//        }
//        catch (JsonException ex)
//        {
//            _logger.LogWarning(ex, "Failed to parse tool result from chat history: {Content}", toolMessage.Content);
//        }
//    }

//    _logger.LogInformation("Reconstructed State: Status={Status}, LastTool={LastTool}",
//        state.Status,
//        state.AdditionalData.TryGetValue("lastCompletedTool", out var tool) ? tool : "none");

//    return state;
//}


//// 1. Focus on State-Changing Messages: It prioritizes tools that update important state values(intent, validation, SKUs, compliance, justification).
//// 2. Response Continuity: For each important tool call, it includes the assistant's response to provide reasoning context.
//// 3. Conversation Flow: It maintains the most recent assistant message to preserve the flow of conversation.
//// 4. Original Order: Messages are added in their original chronological order to maintain conversation coherence.
//// 5. Performance Logging: It logs how much context was pruned for monitoring efficiency.
//private ChatHistory BuildActiveContext(ChatHistory fullHistory, PurchaseRequestState currentState, string userInput)
//{
//    // Create a new, focused context
//    var activeContext = new ChatHistory();

//    // Always start with system prompt
//    activeContext.AddSystemMessage(PromptTemplate.SystemPrompt());

//    // Add only the most important previous messages from history based on state
//    AddRelevantHistoryToContext(activeContext, fullHistory, currentState);

//    // Always add the current user message with state
//    string formattedUserInput = PromptTemplate.UserPrompt()
//        .Replace("{{userInput}}", userInput)
//        .Replace("{{workflowState}}", JsonSerializer.Serialize(currentState, _jsonOptions));

//    activeContext.AddUserMessage(formattedUserInput);

//    return activeContext;
//}

//private void AddRelevantHistoryToContext(ChatHistory activeContext, ChatHistory fullHistory, PurchaseRequestState currentState)
//{
//    // If history is empty or very short, no pruning needed
//    if (fullHistory.Count <= 3) return;

//    // Get the last completed tool for workflow context
//    string lastCompletedTool = currentState.AdditionalData.TryGetValue("lastCompletedTool", out var tool)
//        ? tool.ToString() ?? string.Empty
//        : string.Empty;

//    // Keep track of key messages to include
//    var messagesToInclude = new List<(int Index, ChatMessageContent Message)>();

//    // First, identify key tool responses that represent state transitions
//    for (int i = 0; i < fullHistory.Count; i++)
//    {
//        var message = fullHistory[i];

//        // Always include tool responses (they contain critical state information)
//        if (message.Role == AuthorRole.Tool)
//        {
//            // Parse to check if it's a relevant tool (one that set important state)
//            if (!string.IsNullOrEmpty(message.Content))
//            {
//                try
//                {
//                    var toolResult = JsonNode.Parse(message.Content);

//                    // Include if it's part of the critical workflow path
//                    bool isRelevant =
//                        (toolResult?["intent"] != null) || // ClassifyIntentTool
//                        (toolResult?["isWorkplaceComputer"] != null) || // ValidateProductTool
//                        (toolResult?["sku"] != null) || // ExtractDetailsTool
//                        (toolResult?["compliant"] != null) || // CheckComplianceTool
//                        (toolResult?["justification_approved"] != null); // JustifyApprovalTool

//                    if (isRelevant)
//                    {
//                        // Add this tool response and also the next assistant message (if exists)
//                        messagesToInclude.Add((i, message));

//                        // Include the response to this tool call (the next assistant message)
//                        if (i + 1 < fullHistory.Count && fullHistory[i + 1].Role == AuthorRole.Assistant)
//                        {
//                            messagesToInclude.Add((i + 1, fullHistory[i + 1]));
//                        }
//                    }
//                }
//                catch
//                {
//                    // If we can't parse it, just ignore this message
//                }
//            }
//        }
//    }

//    // Always include the most recent assistant message for continuity
//    for (int i = fullHistory.Count - 1; i >= 0; i--)
//    {
//        if (fullHistory[i].Role == AuthorRole.Assistant)
//        {
//            // Check if we already included this message
//            if (!messagesToInclude.Any(m => m.Index == i))
//            {
//                messagesToInclude.Add((i, fullHistory[i]));
//            }
//            break;
//        }
//    }

//    // Add relevant messages to active context in original order
//    foreach (var (_, message) in messagesToInclude.OrderBy(m => m.Index))
//    {
//        activeContext.AddMessage(message.Role, message.Content);
//    }

//    // Log how much we pruned
//    _logger.LogInformation(
//        "Context pruning: reduced from {OriginalCount} to {PrunedCount} messages",
//        fullHistory.Count,
//        activeContext.Count);
//}


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
//        private static class PromptTemplate
//        {
//            public static string SystemPrompt()
//            {
//                return @"You are a goal-driven, autonomous procurement agent. 
//Your primary purpose is to manage employee purchase order requests from start to finish  by making intelligent, sequential use of the tools provided.

//Tools

//You may use the following tools:

//  1. ClassifyIntentTool – Classifies an employee's request into a specific category: Request product, Show supported products, show product specs, show procurement policies.
//  2. ValidateProductTool - Acts as a gatekeeper for the 'Request product' workflow to confirm the requested item is a workplace computer.
//  3. ExtractDetailsTool – Extracts specific details like model, quantity, SKUs from a validated purchase request.
//  4. CheckComplianceTool – Review the request against all applicable procurement policies.
//  5. JustifyApprovalTool – Evaluates the justification for hardware purchases that violate compliance rules.

//Core Principles:

//  •	Reflect and Plan: After each tool use, reflect on the result and adjust your plan to achieve the goal.
//  •	Reason Step-by-Step: Your internal monologue must show your reasoning for choosing each next action.
//  •	Do Not Guess: If information is missing or a step fails, use your tools to get the information or stop and ask for human approval.
//  •	Expect Structured JSON: All tools will return their results in a structured JSON format. Your next action 
//    must be based on the key-value data contained within this JSON output.

//Workflow Rules:

//  •	Confidence Score Check: If the ClassifyIntentTool returns a confidence score below 0.8, you must stop all other actions. Immediately ask the user for clarification about their request.
//  •	Purchase Request Validation: If the ClassifyIntentTool identifies the intent as 'RequestPurchase', the ONLY AVAILABLE tool for your next step is ValidateProductTool. You are forbidden from using any other tool, including ExtractDetailsTool, until ValidateProductTool has been successfully executed.
//  •	Policy Tool Usage: The CheckComplianceTool can and should be used even if some request information is incomplete. It will determine which policies are applicable based on the available data.
//  •	Justification Requirement: If the CheckComplianceTool returns 'compliant: false', your ONLY next step is to use the JustifyApprovalTool. You must ask the user for a justification first.

//Workflow State Awareness:

//• If you have access to previous workflow state, use it to continue where you left off
//• Do not repeat tools that have already completed successfully  
//• When asking for clarification, include context from previous steps
//• Example: I found MacBook Pro options earlier. Which size do you prefer: 14 inch or 16 inch?
//";
//            }

//            public static string UserPrompt()
//            {
//                return @"
//Previous Workflow State (if any):
//{{workflowState}}

//A new purchase order request has been submitted.

//Request Details:
//{{userInput}}

//Your task is to process this request using the available tools. 
//At each step, select and invoke the tool most appropriate for the current context, and reflect on the output before proceeding. 
//Continue until the purchase order is ready for submission, or stop if the request is invalid, non-compliant, or requires escalation.

//At the end of each interaction, respond ONLY with a valid JSON object containing these fields:

//{
//  ""reflection"": ""(Briefly explain your reasoning or the result for this step.)"",
//  ""nextStep"": ""(What should the agent or user do next? E.g., ask for clarification, proceed to approval, etc.)"",
//  ""userPrompt"": ""(The exact question or instruction for the user. No extra text.)"",
//  ""products"": (If the user must select from a list of products, or if showing available products is helpful, include a JSON array of product objects here. Otherwise, omit this property.)
//}

//Do NOT include any text outside the JSON object.
//";
//            }

//        }

// Capture
//
//
// s the chat history in a formatted string for debugging or logging



//var chatHistoryFetch = string.Join(
//    Environment.NewLine,
//    chatHistory.Select(msg => $"{msg.Role}: {msg.Content}")
//);

//// Load existing purchase request state or create new one
//var requestState = _stateReconstructor.ReconstructStateFromHistory(chatHistory);

//// Add system prompt as the first message if history is empty
//if (chatHistory.Count == 0)
//{
//    chatHistory.AddSystemMessage(PurchaseOrderPrompts.SystemPrompt());
//}

////userInput = PromptTemplate.UserPrompt()
////    .Replace("{{userInput}}", userInput)
////    .Replace("{{workflowState}}", JsonSerializer.Serialize(requestState, _jsonOptions));

//// Add the user's message to the chat history
//chatHistory.AddUserMessage(userInput);

//// Get the chat completion service from the kernel
//var chatService = _kernel.GetRequiredService<IChatCompletionService>();

//// Set up execution settings to auto-invoke kernel functions
//var settings = new OpenAIPromptExecutionSettings
//{
//    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
//    Temperature = 0.0
//};

//// Build a focused context for the model
//var activeContext = _contextPruningService.BuildActiveContext(chatHistory, requestState, userInput);

// Get the AI's response to the active context only
//var result = await chatService.GetChatMessageContentAsync(
//    activeContext,  // <-- Now sending only the relevant context
//    //chatHistory,
//    executionSettings: settings,
//    kernel: _kernel);


//var chatHistorySave = string.Join(
//    Environment.NewLine,
//    chatHistory.Select(msg => $"{msg.Role}: {msg.Content}")
//);


//string completion = result.Content ?? ""; // Get the completion text

//// Add the assistant's response to the chat history
//chatHistory.AddAssistantMessage(completion);

// Save the updated chat history for the session
//await _stateStore.SaveChatHistoryAsync(sessionId, chatHistory);

//return (completion, chatHistory);


//private static string FormatChatHistory(ChatHistory chatHistory, JsonSerializerOptions _jsonOptions)
//{
//    return string.Join(
//        Environment.NewLine + new string('-', 40) + Environment.NewLine,
//        chatHistory.Select(msg =>
//            JsonSerializer.Serialize(new
//            {
//                Role = msg.Role.ToString(),
//                Content = msg.Content
//            }, _jsonOptions)
//        )
//    );
//}







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