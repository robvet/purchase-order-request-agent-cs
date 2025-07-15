using NearbyCS_API.Models.DTO;

namespace NearbyCS_API.Models
{
    public class AgentResponseDto
    {
        public string Reflection { get; set; } = default!;
        public string NextStep { get; set; } = default!;
        public string UserPrompt { get; set; } = default!;
        public DebugInfoDto DebugInfo { get; set; } = new();
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
