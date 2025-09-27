using Microsoft.SemanticKernel.ChatCompletion; // For chat completion services
using SingleAgent.Models;
using SingleAgent.Models.DTO;

namespace SingleAgent.Contracts
{
    public interface IPurchaseOrderAgent
    {
        Task<(string completion, ResponseInformationDto responseInformationDto)> ProcessUserRequestAsync(
                   string userPrompt,
                   string sessionId,
                   TelemetryCollector telemetryCollector);
    }
}
