using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel.ChatCompletion;
using NearbyCS_API.Storage.Contract;
using System.Text.Json;

namespace NearbyCS_API.Storage.Providers
{ 
    public class InMemorySessionStateStore : IStateStore
    {
        private readonly ILogger<InMemorySessionStateStore> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        private const string SessionKeyPrefix = "ChatHistory_";

        public InMemorySessionStateStore(ILogger<InMemorySessionStateStore> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }
        
        public Task<ChatHistory?> GetChatHistoryAsync(string sessionId)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var data = session?.GetString(SessionKeyPrefix + sessionId);
            if (data == null) return Task.FromResult<ChatHistory?>(null);
            return Task.FromResult(JsonSerializer.Deserialize<ChatHistory>(data));
        }

        public Task SaveChatHistoryAsync(string sessionId, ChatHistory history)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            var data = JsonSerializer.Serialize(history);
            session?.SetString(SessionKeyPrefix + sessionId, data);
            return Task.CompletedTask;
        }

        public Task DeleteChatHistoryAsync(string sessionId)
        {
            var session = _httpContextAccessor.HttpContext?.Session;
            session?.Remove(SessionKeyPrefix + sessionId);
            return Task.CompletedTask;
        }
    }
}