using Microsoft.AspNetCore.Mvc;
using NearbyCS_API.Contracts;
using NearbyCS_API.Models;
using System.Text.Json;

namespace NearbyCS_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PurchaseOrderRequestController
        : ControllerBase
    {
        private readonly IPurchaseOrderAgent _purchaseOrderAgent ;
        private readonly ILogger<PurchaseOrderRequestController> _logger;
        private readonly TelemetryCollector _telemetryCollector;
        // IHttpContextAccessor is used to access the current HTTP context, allowing us to set cookies
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PurchaseOrderRequestController(ILogger<PurchaseOrderRequestController> logger, 
                                     IPurchaseOrderAgent purchaseOrderAgent,
                                     TelemetryCollector telemetryCollector,
                                     IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), $"Thrown in {GetType().Name}");
            
            _purchaseOrderAgent = purchaseOrderAgent ?? throw new ArgumentNullException(nameof(purchaseOrderAgent), $"Thrown in {GetType().Name}");
            _telemetryCollector = telemetryCollector ?? throw new ArgumentNullException(nameof(telemetryCollector), $"Thrown in {GetType().Name}");
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor), $"Thrown in {GetType().Name}");

            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Items["TelemetryCollector"] = telemetryCollector;
            }
        }

        [HttpGet(Name = "HealthCheck")]
        public async Task<string> HealthCheck()
        {
            return "OK";
        }

        [HttpPost("ProcessPurchaseRequest")]
        public async Task<IActionResult> ProcessPurchaseRequestAsync([FromBody] string userInputPrompt)
        {
            try
            {
                _logger.LogInformation("Logging user prompt for PO Request: {UserPrompt}", userInputPrompt);

                // validate input
                if (string.IsNullOrWhiteSpace(userInputPrompt))
                {   
                    _logger.LogWarning("Empty or null prompt received.");
                    return BadRequest("Prompt cannot be empty or null.");
                }

                // Lightly sanitize user input
                // Remove leading and trailing whitespace characters
                // spaces, tabs, newlines, other Unicode whitespace characters
                userInputPrompt = userInputPrompt.Trim();

                // 1. Get/generate sessionId
                string sessionId = Request.Cookies["SessionId"] ?? Guid.NewGuid().ToString();

                // 2. Call agent - receive completion, history, and agent logs as a tuple
                var (completion, history, agentLogs) = await _purchaseOrderAgent.ProcessUserRequestAsync(userInputPrompt, sessionId, _telemetryCollector);

                // 3. Set sessionId as cookie for tracking history across turns
                Response.Cookies.Append("SessionId", sessionId, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Lax });

                // 4. Inject dynamic telemetry transformation for tool behavior tracking and telemetry
                var toolSteps = InjectDynamicTelemetryTransformation(_telemetryCollector.GetAll().ToList());

                // 5. Map ChatHistory to DTO (Data Transfer Object)
                var response = new AgentResponseDto
                {
                    SessionId = sessionId,
                    Message = completion,
                    History = ChatHistoryMappingExtensions.MapToDto(history),
                    AgentLogs = agentLogs,
                    Telemetry = _telemetryCollector.GetAll().ToList(),
                    ToolSteps = toolSteps
                };

                _logger.LogInformation("User prompt processed successfully: {Response}", response);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception thrown in {Class}: {Exception}", GetType().Name, ex.Message);
                return StatusCode(500, $"An error occurred in {GetType().Name} while processing your request: {ex.Message}");
            }
        }

        /// <summary>
        /// Injects dynamic transformation of raw telemetry into clean, presentation-ready tool step summaries.
        /// Implements SoC with pluggable intelligence.
        /// Instead of hard-coding formatting changes directly into TelemetryFilter, dynamically inject changes
        /// immediately before the completion renders back to caller, leaving the filter pure and less complex, 
        /// while the controller handles custom business logic. Moreover, the pattern enables future custom 
        /// formatting without requiring changes to the underlying filtering logic.
        /// </summary>
        private List<ToolStepSummary> InjectDynamicTelemetryTransformation(List<string> telemetryEntries)
        {
            var toolSteps = new List<ToolStepSummary>();
            
            try
            {
                var currentStep = new ToolStepSummary();
                var hasActiveStep = false;
                var parentToolName = string.Empty; // Track parent tool name for nested calls

                foreach (var entry in telemetryEntries)
                {
                    // Identity tool calls 
                    if (entry.StartsWith("[TOOL_CALL]"))
                    {
                        // Parse the tool name from the JSON
                        string toolName = "";
                        try
                        {
                            // If a tool call, store the tool name as the parent so that we can merge nested calls
                            var jsonPart = entry.Substring("[TOOL_CALL]".Length).Trim();
                            var toolCallData = JsonSerializer.Deserialize<JsonElement>(jsonPart);
                            
                            if (toolCallData.TryGetProperty("ToolName", out var toolNameElement))
                            {
                                var fullToolName = toolNameElement.GetString() ?? "";
                                // Extract just the tool name (e.g., "ClassifyRequest" from "ClassifyRequestTool.ClassifyRequest")
                                var parts = fullToolName.Split('.');
                                toolName = parts.Length > 1 ? parts[1] : fullToolName;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse tool call JSON");
                            toolName = "Unknown Tool";
                        }

                        // Each Semantic Kernel internally calls kernel.InvokePromptAsync(), creating a nested function call.
                        // The telemetry filter captures both calls resulting into two distinct entries:
                        //  • Parent: ClassifyRequestTool.ClassifyRequest(empty results)
                        //  • Child: InvokePromptAsync_de60780b7c1b45259ac38332998647a2(actual results)



                        // Check if this is an internal InvokePromptAsync call
                        // When we see an InvokePromptAsync_ call, we recognize it as a nested call
                        if (toolName.StartsWith("InvokePromptAsync_"))
                        {
                            // This is a nested call - don't create a new step, just update the current one
                            // Instead of creating a new step, we keep the current step but ensure it uses the parent's name
                            // The parent call provides the friendly tool name
                            if (hasActiveStep && !string.IsNullOrEmpty(parentToolName))
                            {
                                // Keep the parent tool name, but this nested call will provide the results
                                currentStep.ToolName = parentToolName;
                            }
                            else
                            {
                                // No parent context, treat as unknown
                                currentStep.ToolName = "Unknown Tool";
                                hasActiveStep = true;
                            }
                        }
                        else
                        {
                            // This is a real tool call
                            
                            // If we have an active step, add it before starting a new one
                            if (hasActiveStep)
                            {
                                toolSteps.Add(currentStep);
                            }

                            // Start a new step
                            currentStep = new ToolStepSummary();
                            currentStep.ToolName = toolName;
                            parentToolName = toolName; // Remember this for potential nested calls
                            hasActiveStep = true;
                        }
                    }
                    // Look for tool JSON results
                    // The nested call (InvokePromptAsync_) provides the actual JSON result and agent response
                    // Merge them into one clean entry
                    else if (entry.StartsWith("[TOOL_JSON_RESULT]") && hasActiveStep)
                    {
                        try
                        {
                            // Extract the JSON part after the tool name
                            var jsonPart = entry.Substring("[TOOL_JSON_RESULT]".Length).Trim();
                            var colonIndex = jsonPart.IndexOf(':');
                            if (colonIndex > 0)
                            {
                                currentStep.JsonResult = jsonPart.Substring(colonIndex + 1).Trim();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse tool JSON result");
                        }
                    }
                    // Look for agent responses
                    else if (entry.StartsWith("[AGENT_RESPONSE]") && hasActiveStep)
                    {
                        currentStep.AgentResponse = entry.Substring("[AGENT_RESPONSE]".Length).Trim();
                        
                        // This completes the step
                        toolSteps.Add(currentStep);
                        hasActiveStep = false;
                        parentToolName = string.Empty; // Reset parent context
                    }
                }

                // Add the last step if it exists
                if (hasActiveStep)
                {
                    toolSteps.Add(currentStep);
                }

                // Final cleanup: Remove any steps that are just InvokePromptAsync calls without proper parent context
                toolSteps = toolSteps.Where(step => 
                    !step.ToolName.StartsWith("InvokePromptAsync_") && 
                    !string.IsNullOrEmpty(step.ToolName) &&
                    step.ToolName != "Unknown Tool").ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing telemetry to tool steps");
            }

            return toolSteps;
        }
    }
}