using Microsoft.SemanticKernel.ChatCompletion; // For chat completion services
using SingleAgent.Models;

namespace SingleAgent.Contracts
{
    public interface IPurchaseOrderAgent
    {
        Task<(string completion, ChatHistory History)> ProcessUserRequestAsync(
                   string userPrompt,
                   string sessionId,
                   TelemetryCollector telemetryCollector);
    }
}
