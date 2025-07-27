using System.Text.Json.Nodes;

namespace SingleAgent.Models.DTO
{
    public static class AgentResponseDtoExtensions
    {
        public static AgentResponseDto ToAgentResponseDto(
            this JsonNode jsonNode,
            string sessionId,
            List<ChatMessageDto> history,
            List<string> telemetry,
            List<ToolStepSummary> toolSteps,
            bool showDebug)
        {
            var nextStep = jsonNode?["nextStep"]?.ToString();
            var productsNode = jsonNode?["products"];
            var reflection = jsonNode?["reflection"]?.ToString();
            var userPrompt = jsonNode?["userPrompt"]?.ToString();

            if (! showDebug)
            {
                return new AgentResponseDto
                {
                    Reflection = reflection,
                    UserPrompt = userPrompt,
                    Products = productsNode
                };
            }
            else
            {
                return new AgentResponseDto
                {
                    Reflection = reflection,
                    UserPrompt = userPrompt,
                    NextStep = nextStep,
                    Products = productsNode,
                    DebugInfo = new DebugInfoDto
                    {
                        SessionId = sessionId,
                        History = history,
                        Telemetry = telemetry,
                        ToolSteps = toolSteps
                    }
                };
            }
        }
    }
}
