using Microsoft.SemanticKernel.ChatCompletion;
using SingleAgent.Models.DTO;

namespace SingleAgent.Models
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
