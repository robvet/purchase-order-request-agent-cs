using Microsoft.SemanticKernel.ChatCompletion;

namespace NearbyCS_API.Models
{
    public static class ChatHistoryMappingExtensions
    {
        public static List<ChatMessageDto> MapToDto(this ChatHistory history)
        {
            return history.Select(m => new ChatMessageDto
            {
                Role = m.Role.Label,
                Content = m.Content
            }).ToList();
        }
    }
}
