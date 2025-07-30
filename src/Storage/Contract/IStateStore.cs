using Microsoft.SemanticKernel.ChatCompletion;

namespace SingleAgent.Storage.Contract
{
    public interface IStateStore
    {
        Task<ChatHistory?> GetChatHistoryAsync(string sessionId);
        Task SaveChatHistoryAsync(string sessionId, ChatHistory history);
        Task DeleteChatHistoryAsync(string sessionId);
        
        // NEW: Purchase request state management
        Task<PurchaseRequestState?> GetRequestStateAsync(string sessionId);
        Task SaveRequestStateAsync(string sessionId, PurchaseRequestState state);
        Task DeleteRequestStateAsync(string sessionId);
    }

    public class PurchaseRequestState
    {
        public string? Intent { get; set; }
        public string? RequestedModel { get; set; }
        public List<string>? MatchedSkus { get; set; }
        public int? Quantity { get; set; }
        public string? Department { get; set; }
        public decimal? UnitCost { get; set; }
        public string? Status { get; set; } // "extracting", "policy_check", "compliant", etc.
        public Dictionary<string, object>? AdditionalData { get; set; }
    }
}
