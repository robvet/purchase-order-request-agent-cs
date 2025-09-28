using Microsoft.SemanticKernel.ChatCompletion; // For chat completion services
using SingleAgent.Models;
using SingleAgent.Models.DTO;

namespace SingleAgent.Contracts
{
    public interface IPurchaseOrderAgent
    {
        Task<AgentResponseDto> ProcessUserRequestAsync(
                   string userPrompt,
                   string sessionId);
    }
}
