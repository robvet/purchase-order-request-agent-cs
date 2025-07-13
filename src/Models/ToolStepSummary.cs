namespace NearbyCS_API.Models
{
    /// <summary>
    /// Represents a single tool execution step with its result and agent response
    /// </summary>
    public class ToolStepSummary
    {
        /// <summary>
        /// The name of the tool that was called (e.g., "ClassifyRequest", "CheckBudget")
        /// </summary>
        public string ToolName { get; set; } = string.Empty;

        /// <summary>
        /// The JSON result returned by the tool
        /// </summary>
        public string JsonResult { get; set; } = string.Empty;

        /// <summary>
        /// The agent's response/reflection after the tool call
        /// </summary>
        public string AgentResponse { get; set; } = string.Empty;
    }
}