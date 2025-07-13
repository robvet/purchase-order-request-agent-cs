using Microsoft.SemanticKernel.ChatCompletion;

namespace NearbyCS_API.Storage.Contract
{
    public interface IStateStore
    {
        Task<ChatHistory?> GetChatHistoryAsync(string sessionId);
        Task SaveChatHistoryAsync(string sessionId, ChatHistory history);
        Task DeleteChatHistoryAsync(string sessionId);
    }
}
