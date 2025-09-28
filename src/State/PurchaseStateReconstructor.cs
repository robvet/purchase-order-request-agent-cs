using Microsoft.SemanticKernel.ChatCompletion;
using SingleAgent.Agents;
using SingleAgent.Storage.Contract;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SingleAgent.State
{
    //public class PurchaseStateReconstructor
    //{
    //    private readonly ILogger<PurchaseStateReconstructor> _logger;

    //    public PurchaseStateReconstructor(ILogger<PurchaseStateReconstructor> logger)
    //    {
    //        _logger = logger;
    //    }

    //    // Allows agent to maintain stateful behavior across conversation turns without needing a separate state database.
    //    // Uses chat history as the source of truth, reconstructing current purchase state at the start of each turn, and
    //    // inserts at the beginning of user prompt. Analyzes tool outputs in chat history to rebuild state.
    //    // Tracks workflow progression by setting the status and "lastCompletedTool" in AdditionalData.
    //    internal PurchaseRequestState ReconstructStateFromHistory(ChatHistory chatHistory)
    //    {
    //        var state = new PurchaseRequestState
    //        {
    //            AdditionalData = new Dictionary<string, object>()
    //        };

    //        var toolMessages = chatHistory.Where(m => m.Role == AuthorRole.Tool).ToList();

    //        foreach (var toolMessage in toolMessages)
    //        {
    //            if (string.IsNullOrEmpty(toolMessage.Content)) continue;

    //            try
    //            { 
    //                var toolResult = JsonNode.Parse(toolMessage.Content);
    //                if (toolResult == null) continue;

    //                // Generic data points
    //                if (toolResult["intent"] != null)
    //                {
    //                    state.Intent = toolResult["intent"]?.ToString();
    //                    state.AdditionalData["lastCompletedTool"] = "ClassifyIntentTool";
    //                    state.Status = "classified";
    //                }
    //                if (toolResult["is_workplace_computer"]?.GetValue<bool>() == true)
    //                {
    //                    state.AdditionalData["lastCompletedTool"] = "ValidateProductTool";
    //                    state.Status = "validated";
    //                }
    //                if (toolResult["sku"] != null && toolResult["sku"] is JsonArray skuArray)
    //                {
    //                    state.MatchedSkus = skuArray.Select(s => s?.ToString()).Where(s => !string.IsNullOrEmpty(s)).ToList()!;
    //                    state.AdditionalData["lastCompletedTool"] = "ExtractDetailsTool";
    //                    state.Status = "extracted";
    //                }
    //                if (toolResult["quantity"] != null)
    //                {
    //                    state.Quantity = toolResult["quantity"]?.GetValue<int>();
    //                }
    //                if (toolResult["department"] != null)
    //                {
    //                    state.Department = toolResult["department"]?.ToString();
    //                }
    //                if (toolResult["compliant"] != null)
    //                {
    //                    state.AdditionalData["lastCompletedTool"] = "CheckComplianceTool";
    //                    state.Status = toolResult["compliant"]?.GetValue<bool>() == true ? "compliant" : "awaiting_justification";
    //                }
    //                if (toolResult["justification_approved"] != null)
    //                {
    //                    state.AdditionalData["lastCompletedTool"] = "JustifyApprovalTool";
    //                    state.Status = toolResult["justification_approved"]?.GetValue<bool>() == true ? "justification_approved" : "justification_rejected";
    //                }
    //            }
    //            catch (JsonException ex)
    //            {
    //                _logger.LogWarning(ex, "Failed to parse tool result from chat history: {Content}", toolMessage.Content);
    //            }
    //        }

    //        _logger.LogInformation("Reconstructed State: Status={Status}, LastTool={LastTool}",
    //            state.Status,
    //            state.AdditionalData.TryGetValue("lastCompletedTool", out var tool) ? tool : "none");

    //        return state;
    //    }
    //}
}
