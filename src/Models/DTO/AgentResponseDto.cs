using SingleAgent.Models.DTO;
using System.Text.Json.Nodes;

namespace SingleAgent.Models
{
    public class AgentResponseDto
    {

        public string? UserPrompt { get; set; }
        public string? Completion { get; set; }
        //public List<ChatMessageDto> Traces { get; set; } = new    
        //public List<ChatMessageDto> ChatMessageDtos { get; set; } = new();
        public TraceDetail TraceDetail { get; set; }
        public List<MessageThreadModel> MessageThreads = new();
    }

    //public class ExecutionTraceLog
    //{
    //    public string SessionId { get; set; } = default!;
    //    public List<ChatMessageDto> History { get; set; } = new();
    //    //public List<string> AgentLogs { get; set; } = new();
    //    //public List<string> Telemetry { get; set; } = new();
    //    //public List<ToolStepSummary> ToolSteps { get; set; } = new();
    //}

    public class TraceDetail
    {
        public List<string> FormattedOutput { get; set; } = new();
    }
}


