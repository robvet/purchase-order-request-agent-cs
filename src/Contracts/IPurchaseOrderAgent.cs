using Microsoft.SemanticKernel.ChatCompletion; // For chat completion services
using NearbyCS_API.Models;

namespace NearbyCS_API.Contracts
{
    public interface IPurchaseOrderAgent
    {
        Task<(string completion, ChatHistory History, List<string> AgentLogs)> ProcessUserRequestAsync(
                   string userPrompt,
                   string sessionId,
                   TelemetryCollector telemetryCollector);
    }
}
