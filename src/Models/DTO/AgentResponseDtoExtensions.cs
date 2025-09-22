using System.Text.Json.Nodes;

namespace SingleAgent.Models.DTO
{
    public static class AgentResponseDtoExtensions
    {
        //public static AgentResponseDto ToAgentResponseDto(
        //    this JsonNode jsonNode,
        //    string sessionId,
        //    List<ChatMessageDto> history)
        //    //List<string> telemetry,
        //    //List<ToolStepSummary> toolSteps)
        //{
        //    var completion = jsonNode?["completion"]?.ToString();
        //    var userPrompt = jsonNode?["userPrompt"]?.ToString();

        //    return new AgentResponseDto
        //    {
        //        Completion = completion,
        //        UserPrompt = userPrompt,
        //        Traces= history
        //        //ExecutionTraceLog = new ExecutionTraceLog
        //        //{
        //        //    SessionId = sessionId,
        //        //    History = history
        //        //    //Telemetry = telemetry,
        //        //    //ToolSteps = toolSteps
        //        //}
        //    };
        //}
    }
}
