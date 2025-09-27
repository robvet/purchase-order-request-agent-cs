using SingleAgent.Models.DTO;
using SingleAgent.Models.Enums;
using System.Text.Json.Nodes;

namespace SingleAgent.Models
{
    public class AgentResponseDto
    {
        public AgentResponseDto(int executionTime, Role role, string content, int inputTokens, int outputTokens, 
                                int reasoningTokens, List<ToolStepSummaryModel> toolStepSummary, TraceDetail traceDetail)
        {
            ExecutionTime = executionTime;
            Role = role;
            Content = content;
            InputTokens = inputTokens;
            OutputTokens = outputTokens;
            ReasoningTokens = reasoningTokens;
            ResponseInformationDto = new ResponseInformationDto(0, 0, 0);
            ToolStepSummary = toolStepSummary;

            //StepName = stepName;
            TraceDetail = traceDetail;
            //ToolStepSummary = new List<ToolStepSummary>();
        }

        public int ExecutionTime { get; }
        public Role Role { get; }
        public string Content { get; }
        public int InputTokens { get; }
        public int OutputTokens { get; }
        public int ReasoningTokens { get; }
        public ResponseInformationDto ResponseInformationDto { get; }
        //public string StepName { get; }
        public List<ToolStepSummaryModel> ToolStepSummary { get; set; } = new();
        public TraceDetail TraceDetail { get; } = new();
    }

    public class TraceDetail
    {
        public List<string> FormattedOutput { get; set; } = new();
    }


    //public class ExecutionTraceLog
    //{
    //    public string SessionId { get; set; } = default!;
    //    public List<ChatMessageDto> History { get; set; } = new();
    //    //public List<string> AgentLogs { get; set; } = new();
    //    //public List<string> Telemetry { get; set; } = new();
    //    //public List<ToolStepSummary> ToolSteps { get; set; } = new();
    //}


}


