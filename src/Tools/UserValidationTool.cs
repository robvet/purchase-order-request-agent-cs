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
        [Description("Captures and validates user context including employee email and department for purchase requests.")]
        public class UserValidationTool
        {
            private const string ToolName = "UserValidationTool";
            private readonly ILogger<UserValidationTool> _logger;
            private const string TracePrefix = "*** CUSTOM:"; // add prefix to custom trace messages for easy identification 

            public UserValidationTool(ILogger<UserValidationTool> logger)
            {   
                _logger = logger;
            }

            [KernelFunction]
            [Description("Captures and validates user context details required for the purchase request workflow.")]
            public async Task<string> ValidateUserAsync(
                Kernel kernel,
                [Description("Natural language text that may contain user context details.")] string userRequest,
                [Description("The user intent. This tool should only be used for 'RequestProduct' intents.")] string intent,
                [Description("(REQUIRED) Model's explanation for why user validation is needed at this step")] string reasoning)
            {
                try
                {
                    _logger.LogInformation("{TracePrefix} Capturing userRequest in {ToolName} from request: {UserRequest}", TracePrefix, ToolName, userRequest);
                    _logger.LogInformation("{TracePrefix} Capturing intent in {ToolName} from request: {UserRequest}", TracePrefix, intent, intent);
                    _logger.LogInformation("{TracePrefix} Capturing reasoning in {ToolName} from request: {UserRequest}", TracePrefix, ToolName, reasoning);

                    // Validate parameters
                    if (string.IsNullOrWhiteSpace(userRequest))
                        throw new ArgumentException("User request required in {ToolName}.", ToolName);

                    if (string.IsNullOrWhiteSpace(reasoning))
                        throw new ArgumentException("Reasoning required in {ToolName} for audit trail and tool selection improvement.", ToolName);

                    if (intent != "RequestProduct")
                    {
                        _logger.LogWarning("{TracePrefix} UserValidationTool called with non-purchase intent: {Intent} in {ToolName}", TracePrefix, intent, ToolName);
                        return JsonSerializer.Serialize(new
                        {
                            ToolName = ToolName,
                            status = "error",
                            error = "wrong_intent",
                            intent = intent,
                            message = $"This tool captures user context for purchase requests only. Current intent: '{intent}'.",
                            suggestion = "Use appropriate tool for the current intent.",
                            reasoning = reasoning
                        });
                    }

                    // Extract potential identifiers from request using LLM
                    var prompt = PromptTemplate.ExtractUserContextPrompt(userRequest).Replace("{{userRequest}}", userRequest);  // Add this line to replace placeholder
                    
                    //var prompt = PromptTemplate.ExtractUserContextPrompt(userRequest);
                    var result = await kernel.InvokePromptAsync(prompt, new KernelArguments
                    {
                        ["userRequest"] = userRequest
                    });

                    var extractedContext = JsonNode.Parse(result.ToString());

                    var email = extractedContext?["email"]?.ToString();
                    var department = extractedContext?["department"]?.ToString();

                    // After extracting values
                    var missingFields = new List<string>();
                    if (string.IsNullOrWhiteSpace(email)) missingFields.Add("email");
                    if (string.IsNullOrWhiteSpace(department)) missingFields.Add("department");

                    object response;
                    if (missingFields.Any())
                    {
                        response = new
                        {
                            toolname = ToolName,
                            status = "rerun",
                            user_context = new
                            {
                                email = email,
                                department = department
                            },
                            missing_fields = missingFields,
                            message = $"Please provide: {string.Join(", ", missingFields)}",
                            confidence = 0.0,
                            reasoning = reasoning,
                            timestamp = DateTime.UtcNow
                        };
                    }
                    else
                    {
                        response = new
                        {
                            toolname = ToolName,
                            status = "completed",
                            user_context = new
                            {
                                email = email,
                                department = department
                            },
                            confidence = 1.0,
                            reasoning = reasoning,
                            timestamp = DateTime.UtcNow
                        };
                    }

                    return JsonSerializer.Serialize(response);



                    //var response = new
                    //{
                    //    toolname = ToolName,
                    //    status = missingFields.Any() ? "incomplete" : "complete",
                    //    user_context = new
                    //    {
                    //        employee_id = employeeId,
                    //        full_name = fullName,
                    //        email = email,
                    //        department = department,
                    //        supervisor_email = supervisorEmail
                    //    },
                    //    missing_fields = missingFields,
                    //    message = missingFields.Any()
                    //        ? $"Please provide: {string.Join(", ", missingFields)}"
                    //        : null,
                    //    confidence = 1.0,
                    //    reasoning = reasoning,
                    //    timestamp = DateTime.UtcNow,
                    //    correlationId = Guid.NewGuid().ToString()
                    //};


                    //var response = new
                    //{
                    //    ToolName = ToolName,
                    //    status = "complete",
                    //    user_context = new
                    //    {
                    //        employee_id = employeeId,
                    //        full_name = fullName,
                    //        email = email,
                    //        department = department,
                    //        supervisor_email = supervisorEmail
                    //    },
                    //    confidence = 1.0,
                    //    reasoning = reasoning,
                    //    timestamp = DateTime.UtcNow,
                    //    correlationId = Guid.NewGuid().ToString()
                    //};

                    //return JsonSerializer.Serialize(response);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{TracePrefix} Error in {ToolName}", TracePrefix, ToolName);
                    return JsonSerializer.Serialize(new
                    {
                        ToolName = ToolName,
                        status = "rerun",
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
- Department name

Return STRICTLY valid JSON with these fields:
{
    ""status"": ""complete"" | ""incomplete"",
    ""email"": ""extracted email or null"",
    ""department"": ""extracted department or null"",
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