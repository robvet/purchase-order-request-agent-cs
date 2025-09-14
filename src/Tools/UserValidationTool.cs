namespace SingleAgent.Tools
{
    using global::SingleAgent.Contracts;
    using Microsoft.SemanticKernel;
    using Models.DTO;
    using System.ComponentModel;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    namespace SingleAgent.Tools
    {
        [Description("Captures and validates user context including employee details, department, and approval chain for purchase requests.")]
        public class UserValidationTool
        {
            private const string ToolName = "UserValidationTool";
            private readonly ILogger<UserValidationTool> _logger;

            public UserValidationTool(ILogger<UserValidationTool> logger)
            {   
                _logger = logger;
            }

            [KernelFunction]
            [Description("Captures and validates user context details required for the purchase request workflow.")]
            public async Task<string> ValidateUserAsync(
                Kernel kernel,
                [Description("Natural language text that may contain user context details.")] string userRequest,
                [Description("The user intent. This tool should only be used for 'RequestPurchase' intents.")] string intent,
                [Description("(REQUIRED) Model's explanation for why user validation is needed at this step")] string reasoning)
            {
                try
                {
                    _logger.LogInformation("Capturing userRequest in {ToolName} from request: {UserRequest}", ToolName, userRequest);
                    _logger.LogInformation("Capturing intent in {ToolName} from request: {UserRequest}", intent, intent);
                    _logger.LogInformation("Capturing reasoning in {ToolName} from request: {UserRequest}", ToolName, reasoning);

                    // Validate parameters
                    if (string.IsNullOrWhiteSpace(userRequest))
                        throw new ArgumentException("User request required in {ToolName}.", ToolName);

                    if (string.IsNullOrWhiteSpace(reasoning))
                        throw new ArgumentException("Reasoning required in {ToolName} for audit trail and tool selection improvement.", ToolName);

                    if (intent != "RequestPurchase")
                    {
                        _logger.LogWarning("UserValidationTool called with non-purchase intent: {Intent} in {ToolName}", intent, ToolName);
                        return JsonSerializer.Serialize(new
                        {
                            ToolName = ToolName,
                            status = "error",
                            error = "wrong_tool",
                            intent = intent,
                            message = $"This tool captures user context for purchase requests only. Current intent: '{intent}'.",
                            suggestion = "Use appropriate tool for the current intent.",
                            reasoning = reasoning
                        });
                    }

                    // Extract potential identifiers from request using LLM
                    var prompt = PromptTemplate.ExtractUserContextPrompt(userRequest);
                    var result = await kernel.InvokePromptAsync(prompt, new KernelArguments
                    {
                        ["userRequest"] = userRequest
                    });

                    var extractedContext = JsonNode.Parse(result.ToString());

                    var employeeId = extractedContext?["employeeId"]?.ToString();
                    var fullName = extractedContext?["fullname"]?.ToString();
                    var email = extractedContext?["email"]?.ToString();
                    var department = extractedContext?["department"]?.ToString();
                    var supervisorEmail = extractedContext?["supervisorEmail"]?.ToString();

                    var response = new
                    {
                        ToolName = ToolName,
                        status = "complete",
                        user_context = new
                        {
                            employee_id = employeeId,
                            full_name = fullName,
                            email = email,
                            department = department,
                            supervisor_email = supervisorEmail
                        },
                        confidence = 1.0,
                        reasoning = reasoning,
                        timestamp = DateTime.UtcNow,
                        correlationId = Guid.NewGuid().ToString()
                    };

                    return JsonSerializer.Serialize(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in {ToolName}", ToolName);
                    return JsonSerializer.Serialize(new
                    {
                        ToolName = ToolName,
                        status = "error",
                        error = "processing_error",
                        message = "Failed to process user context details.",
                        details = ex.Message,
                        reasoning = reasoning
                    });
                }
            }

            private static class PromptTemplate
            {
                public static string ExtractUserContextPrompt(string userRequest)
                {
                    return @"Extract user identifying information from the request.

User request: {{userRequest}}

Look for and extract:
- Email address
- Employee ID
- Full Name
- Department name
- Supervisor's email

Return STRICTLY valid JSON with these fields:
{
    ""status"": ""complete"" | ""incomplete"",
    ""email"": ""extracted email or null"",
    ""employeeId"": ""extracted employee ID or null"",
    ""fullName"": ""extracted full name or null"",
    ""department"": ""extracted department or null"",
    ""supervisorEmail"": ""extracted supervisor email or null"",
    ""missing_fields"": [""array of required fields that are null""]
}

If ANY field is null, status must be 'incomplete' and field name added to missing_fields array.
Return ONLY the JSON object, no additional text or explanations.";
                }
            }
        }
       
    }
}


//********************************************************************
//* Future Code
//********************************************************************


//public UserValidationTool(ILogger<UserValidationTool> logger, IUserDirectoryService userDirectory)
//{
//    _logger = logger;
//    _userDirectory = userDirectory;
//}

//// Interface for user directory service
//public interface IUserDirectoryService
//{
//    Task<UserDetails?> GetUserDetailsAsync(string? email, string? employeeId);
//    Task<ApprovalChain> GetApprovalChainAsync(string employeeId);
//}

//public class UserDetails
//{
//    public string Email { get; set; } = string.Empty;
//    public string EmployeeId { get; set; } = string.Empty;
//    public string FullName { get; set; } = string.Empty;
//    public string Department { get; set; } = string.Empty;
//    public string CostCenter { get; set; } = string.Empty;
//}

//public class ApprovalChain
//{
//    public string SupervisorEmail { get; set; } = string.Empty;
//    public string SupervisorName { get; set; } = string.Empty;
//    public List<string> ApproverEmails { get; set; } = new();
//}

// Validate that each field is populated


//// Validate against directory service
//var userDetails = await _userDirectory.GetUserDetailsAsync(email, employeeId);
//if (userDetails == null)
//{
//    return JsonSerializer.Serialize(new
//    {
//        status = "error",
//        error = "user_not_found",
//        message = "Unable to validate user details. Please provide your email or employee ID.",
//        required_fields = new[] { "email", "employeeId" }
//    });
//}

// Get approval chain
//var approvalChain = await _userDirectory.GetApprovalChainAsync(userDetails.EmployeeId);