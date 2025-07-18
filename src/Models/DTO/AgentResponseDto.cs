using NearbyCS_API.Models.DTO;
using System.Text.Json.Nodes;

namespace NearbyCS_API.Models
{
    public class AgentResponseDto
    {
        public string? UserPrompt { get; set; }
        public string? Reflection { get; set; }
        public string? NextStep { get; set; }
        public JsonNode? Products { get; set; }
        public DebugInfoDto? DebugInfo { get; set; }
    }

    public class DebugInfoDto
    {
        public string SessionId { get; set; } = default!;
        public List<ChatMessageDto> History { get; set; } = new();
        public List<string> AgentLogs { get; set; } = new();
        public List<string> Telemetry { get; set; } = new();
        public List<ToolStepSummary> ToolSteps { get; set; } = new();
    }
}
