using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SingleAgent.Agents;
using SingleAgent.Prompting;
using SingleAgent.Storage.Contract;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SingleAgent.Context
{
    public class ContextPruningService
    {
        private readonly ILogger<ContextPruningService> _logger; // Logger for this agent
        //private readonly JsonSerializerOptions _jsonOptions;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ContextPruningService(ILogger<ContextPruningService> logger)
        {
            _logger = logger;
        }

        // 1. Focus on State-Changing Messages: It prioritizes tools that update important state values(intent, validation, SKUs, compliance, justification).
        // 2. Response Continuity: For each important tool call, it includes the assistant's response to provide reasoning context.
        // 3. Conversation Flow: It maintains the most recent assistant message to preserve the flow of conversation.
        // 4. Original Order: Messages are added in their original chronological order to maintain conversation coherence.
        // 5. Performance Logging: It logs how much context was pruned for monitoring efficiency.
        internal ChatHistory BuildModelContext(ChatHistory fullHistory)
        {
            try
            {
                // Create a new, focused context
                var modelContext = new ChatHistory();

                // 2. Get only relevant messages (last state-changing sequence)
                var relevantMessages = GetRelevantMessages(fullHistory);
                foreach (var message in relevantMessages)
                {
                    modelContext.AddMessage(message.Role, message.Content);
                }

                // 3. Add current user input (without injecting state)
                //modelContext.AddUserMessage(userInput);

                _logger.LogInformation(
                    "Context pruning: from {OriginalCount} to {FilteredCount} messages",
                    fullHistory.Count,
                    modelContext.Count);

                return modelContext;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to prune modexContext from the Agent");
                var error = new
                {
                    method = "BuildModelContext",
                    error = "exception",
                    details = ex.Message,
                    timestamp = DateTime.UtcNow
                };
                throw;
            }
            
            
            

            //// Add only the most important previous messages from history based on state
            //AddRelevantHistoryToContext(modelContext, fullHistory, currentState);

            //// Always add the current user message with state
            //string formattedUserInput = PurchaseOrderPrompts.UserPrompt()
            //    .Replace("{{userInputPrompt}}", userInput)
            //    .Replace("{{workflowState}}", JsonSerializer.Serialize(currentState, _jsonOptions));

            //modelContext.AddUserMessage(formattedUserInput);

            //return modelContext;
        }

        private IEnumerable<ChatMessageContent> GetRelevantMessages(ChatHistory history)
        {
            // For short conversations, return the messages themselves
            if (history.Count <= 3)
            {
                return history;
            }

            // Get last meaningful interaction (tool call + response)
            var lastMessages = history
                .Reverse()
                .Take(3)  // Last user input, tool result, and assistant response
                .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                .ToList();

            return lastMessages;
        }





        //// Could make helper methods static if they don't need instance state
        //private static bool IsRelevantToolResponse(JsonNode toolResult)
        //{
        //    return (toolResult?["intent"] != null) ||
        //           (toolResult?["isWorkplaceComputer"] != null) ||
        //           // etc.
        //}


        private void AddRelevantHistoryToContext(ChatHistory activeContext, ChatHistory fullHistory, PurchaseRequestState currentState)
        {
            // Add system prompt in BuildActiveContext already, so we only need the meaningful history

            // Filter out unnecessary messages that add no value:
            // 1. Tool calls with no state-changing information
            // 2. Repetitive assistant acknowledgments
            // 3. Internal debugging information

            foreach (var message in fullHistory)
            {
                bool shouldInclude = true;

                // Skip system messages (already added in BuildActiveContext)
                if (message.Role == AuthorRole.System)
                {
                    shouldInclude = false;
                }

                // Skip empty or whitespace-only messages
                if (string.IsNullOrWhiteSpace(message.Content))
                {
                    shouldInclude = false;
                }

                // Skip tool messages that contain no actual state changes
                if (message.Role == AuthorRole.Tool && !string.IsNullOrEmpty(message.Content))
                {
                    try
                    {
                        var toolResult = JsonNode.Parse(message.Content);

                        // Keep tool responses with relevant fields
                        if (toolResult is JsonObject obj)
                        {
                            // Default: exclude simple status messages
                            bool shouldExclude = obj.Count <= 1 &&
                                (obj.ContainsKey("status") || obj.ContainsKey("message") || obj.ContainsKey("error"));

                            // But always keep intent classification results
                            if (obj.ContainsKey("intent") || obj.ContainsKey("confidence"))
                            {
                                shouldExclude = false;
                            }

                            shouldInclude = !shouldExclude;
                        }
                    }
                    catch
                    {
                        // If we can't parse, include it to be safe
                    }
                }

                // Include the message if it passed all filters
                if (shouldInclude)
                {
                    activeContext.AddMessage(message.Role, message.Content);
                }
            }

            _logger.LogInformation(
                "Context filtering: removed unnecessary messages, reduced from {OriginalCount} to {FilteredCount} messages",
                fullHistory.Count,
                activeContext.Count);



            //_logger.LogInformation("History count: {Count}, HasLastCompletedTool: {HasTool}",
            //    fullHistory.Count,
            //    currentState.AdditionalData.ContainsKey("lastCompletedTool"));

            //// Log state
            //foreach (var pair in currentState.AdditionalData)
            //{
            //    _logger.LogInformation("State: {Key}={Value}", pair.Key, pair.Value);
            //}


            //// If history is empty or very short, no pruning needed
            //if (fullHistory.Count <= 5)
            //{
            //    foreach (var message in fullHistory)
            //    {
            //        modelContext.AddMessage(message.Role, message.Content);
            //    }
            //    return;
            //}

            //// Get the last completed tool for workflow context
            ////string lastCompletedTool = currentState.AdditionalData.TryGetValue("lastCompletedTool", out var tool)
            ////    ? tool.ToString() ?? string.Empty
            ////    : string.Empty;

            //// Keep track of key messages to include
            //var messagesToInclude = new List<(int Index, ChatMessageContent Message)>();

            //// First, identify key tool responses that represent state transitions
            //for (int i = 0; i < fullHistory.Count; i++)
            //{
            //    var message = fullHistory[i];

            //    // Always include tool responses (they contain critical state information)
            //    if (message.Role == AuthorRole.Tool)
            //    {
            //        // Parse to check if it's a relevant tool (one that set important state)
            //        if (!string.IsNullOrEmpty(message.Content))
            //        {
            //            try
            //            {
            //                var toolResult = JsonNode.Parse(message.Content);

            //                //// Include if it's part of the critical workflow path
            //                //bool isRelevant =
            //                //    (toolResult?["intent"] != null) || // ClassifyIntentTool
            //                //    (toolResult?["isWorkplaceComputer"] != null) || // ValidateProductTool
            //                //    (toolResult?["sku"] != null) || // ExtractDetailsTool
            //                //    (toolResult?["compliant"] != null) || // CheckComplianceTool
            //                //    (toolResult?["justification_approved"] != null); // JustifyApprovalTool

            //                bool isRelevant = IsToolResultRelevant(toolResult);

            //                if (isRelevant)
            //                {
            //                    // Add this tool response and also the next assistant message (if exists)
            //                    messagesToInclude.Add((i, message));

            //                    // Include the response to this tool call (the next assistant message)
            //                    if (i + 1 < fullHistory.Count && fullHistory[i + 1].Role == AuthorRole.Assistant)
            //                    {
            //                        messagesToInclude.Add((i + 1, fullHistory[i + 1]));
            //                    }
            //                }
            //            }
            //            catch
            //            {
            //                // If we can't parse it, just ignore this message
            //            }
            //        }
            //    }
            //}

            //// Always include the most recent assistant message for continuity
            //for (int i = fullHistory.Count - 1; i >= 0; i--)
            //{
            //    if (fullHistory[i].Role == AuthorRole.Assistant)
            //    {
            //        // Check if we already included this message
            //        if (!messagesToInclude.Any(m => m.Index == i))
            //        {
            //            messagesToInclude.Add((i, fullHistory[i]));
            //        }
            //        break;
            //    }
            //}

            //// Add relevant messages to active context in original order
            //foreach (var (_, message) in messagesToInclude.OrderBy(m => m.Index))
            //{
            //    modelContext.AddMessage(message.Role, message.Content);
            //}

            //// Log how much we pruned
            //_logger.LogInformation(
            //    "Context pruning: reduced from {OriginalCount} to {PrunedCount} messages",
            //    fullHistory.Count,
            //    modelContext.Count);
        }

        private bool IsToolResultRelevant(JsonNode toolResult)
        {
            if (toolResult == null) return false;

            try
            {
                // Any non-empty result is potentially important
                if (toolResult is JsonObject obj && obj.Count > 0)
                {
                    // Iterate through all properties to look for meaningful values
                    foreach (var prop in obj)
                    {
                        var propName = prop.Key;
                        var propValue = prop.Value;

                        // Skip null values
                        if (propValue == null) continue;

                        // Boolean values often represent decisions or state changes
                        if (propValue is JsonValue value &&
                            (value.TryGetValue(out bool _) ||
                             value.TryGetValue(out string strVal) && !string.IsNullOrEmpty(strVal)))
                        {
                            return true;
                        }

                        // Arrays often contain entity lists (products, SKUs, etc.)
                        if (propValue is JsonArray array && array.Count > 0)
                        {
                            return true;
                        }

                        // Nested objects with content are likely important
                        if (propValue is JsonObject nestedObj && nestedObj.Count > 0)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch
            {
                // If we can't parse it properly, err on the side of caution
                return true;
            }
        }

    }
}
