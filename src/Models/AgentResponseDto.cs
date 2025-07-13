namespace NearbyCS_API.Models
{
    public class AgentResponseDto
    {
        public string SessionId { get; set; } = default!;
        public string Message { get; set; } = default!;
        public List<ChatMessageDto> History { get; set; } = new();
        public List<string> AgentLogs { get; set; } = new();
        public List<string> Telemetry { get; set; } = new(); // Add this property for telemetry
        public List<ToolStepSummary> ToolSteps { get; set; } = new(); // Clean tool steps for demo
    }
}
