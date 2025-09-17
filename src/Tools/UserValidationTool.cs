using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using static Azure.Core.HttpHeader;

namespace SingleAgent.Tools
{
    using global::SingleAgent.Contracts;
    using Microsoft.AspNetCore.Http.HttpResults;
    using Microsoft.AspNetCore.Razor.TagHelpers;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.SemanticKernel;
    using Microsoft.VisualBasic;
    using Models.DTO;
    using System.Collections.Generic;
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

            private readonly string[] _departmentNames =
            {
                "Engineering", 
                "Marketing", 
                "Sales", "HR", 
                "Finance", 
                "Information Technology", 
                "Operations", 
                "Customer Support", 
                "Administration", 
                "Legal", 
                "Research and Development"
            };

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
                    var prompt = PromptTemplate.ExtractUserContextPrompt(userRequest)
                        .Replace("{{userRequest}}", userRequest);

                    var result = await kernel.InvokePromptAsync(prompt, new KernelArguments
                    {
                        ["userRequest"] = userRequest,
                    });


                    // In ValidateUserAsync:
                    _logger.LogInformation("INPUT - Raw userRequest: '{UserRequest}'", userRequest);

                    
                    // Debugging Code
                    //// Extract potential identifiers from request using LLM
                    //var prompt2 = PromptTemplate.ExtractUserContextPrompt(userRequest, _departmentNames);

                    //// Capture debug info to variables
                    //var debugInput = $"INPUT - Raw userRequest: '{userRequest}'";
                    //var debugPrompt = $"PROMPT - Full prompt being sent to LLM:\n{prompt}";

                    ////var result2 = await kernel.InvokePromptAsync(prompt, new KernelArguments
                    ////{
                    ////    ["userRequest"] = userRequest
                    ////});

                    //var debugOutput = $"OUTPUT - Raw LLM Response:\n{result}";



                    // Parse LLM response
                    var extractedContext = JsonNode.Parse(result.ToString());

                    // Extract values returned by LLM
                    var email = extractedContext?["email"]?.ToString();

                    //// Validate required top-level properties exist
                    //if (extractedContext?["department"] == null)
                    //{
                    //    throw new InvalidOperationException($"{TracePrefix} LLM response in {ToolName} is missing required top-level properties");
                    //}

                    var departmentInfo = extractedContext?["department"];

                    bool isDepartmentAccepted = false;
                    string? matchedDepartment = null;
                    string? providedDepartment = null;

                    // Determine if department was accepted based on confidence
                    isDepartmentAccepted = departmentInfo?["accepted"]?.GetValue<bool>() ?? false;

                    if (!isDepartmentAccepted)
                    {
                        // extract the provided value for logging
                        providedDepartment = departmentInfo?["provided_value"]?.ToString();
                    }
                    else
                    {
                        // Department accepted, proceed as normal, and extract the matched department
                        matchedDepartment = departmentInfo?["matched_department"]?.ToString();
                    }

                    // log extracted email value
                    if (string.IsNullOrEmpty(email))
                    {
                        _logger.LogWarning("{TracePrefix} User Email not found in user request in {ToolName}", TracePrefix, ToolName);
                    }
                    else
                    {
                        _logger.LogInformation("{TracePrefix} Extracted Email: {Email} in {ToolName}", TracePrefix, email, ToolName);
                    }

                    // log extracted department value

                    if (isDepartmentAccepted)
                    {
                        _logger.LogInformation("{TracePrefix} Extracted Department: {Department} in {ToolName}", TracePrefix, departmentInfo?["matched_department"]?.ToString(), ToolName);
                    }
                    else
                    {
                        _logger.LogWarning("{TracePrefix} Department '{providedDepartment}' not accepted (confidence too low) in {ToolName}", TracePrefix, providedDepartment, ToolName);
                    }

                    //// After extracting values
                    //var missingFields = new List<string>();
                    //if (string.IsNullOrWhiteSpace(email)) missingFields.Add("email");
                    //if (string.IsNullOrWhiteSpace(matchedDepartment)) missingFields.Add("department");

                    // Get missing fields from LLM response
                    var llmMissingFields = extractedContext?["missing_fields"]?
                        .AsArray()
                        .Select(node => node?.GetValue<string>())
                        .Where(field => !string.IsNullOrEmpty(field))
                        .ToList() ?? new List<string>();

                    object response;

                    if (llmMissingFields.Any())
                    {
                        response = new
                        {
                            toolname = ToolName,
                            status = "rerun",
                            user_context = new
                            {
                                email = email,
                                department = matchedDepartment
                            },
                            missing_fields = llmMissingFields,
                            incorrect_department_name = departmentInfo?["accepted"]?.GetValue<bool>() == false ? departmentInfo?["provided_value"]?.ToString() : null,
                            // return department names if department is empty
                            department_names = string.IsNullOrWhiteSpace(providedDepartment) ? _departmentNames : null,
                            message = $"Please provide: {string.Join(", ", llmMissingFields)}",
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
                                department = matchedDepartment
                            },
                            reasoning = reasoning,
                            timestamp = DateTime.UtcNow
                        };
                    }

                    return JsonSerializer.Serialize(response);

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
                        // Clever code from LM - inserts newline, hyphen, space between each department name
                        // - Engineering
                        // -Marketing
                        // - Sales
                        // - HR
                        //var departmentList = string.Join("\n- ", validDepartments);
                        // var departmentList = string.Join("\n", validDepartments.Select(d => d.Trim()));
                        //var departmentList = string.Join("\n", validDepartments.Select(d => $"- {d.Trim()}"));

                    return @"Extract and validate user information from userRequest.

User request: {{userRequest}}

userRequest Processing Rules:
- If userRequest is formatted as 'Email: X, Department: Y', parse both values
- If userRequest contains a single value:
    * If it matches a department name, treat as department only
    * If it matches email format, treat as email only
- Match department names case-insensitive

Valid department names:
-Engineering
- Marketing
- Sales
- HR
- Finance
- Information Technology
- Operations
- Customer Support
- Administration
- Legal
- Research and Development

Department Validation Rules:
-Exact match = 100 % confidence
- Common abbreviations(e.g., 'IT' → 'Information Technology') = 90 % confidence
- Partial matches with clear intent (e.g., 'Tech' → 'Information Technology') = 85% confidence
- Similar terms (e.g., 'R&D' → 'Research and Development') = 80% confidence
- Anything less certain = Below 80% confidence, treat as no match

Return STRICTLY valid JSON with these fields:
{
    {
        ""status"": ""complete"" | ""incomplete"",
    ""email"": ""extracted email or null"",
    ""department"": {
            {
                ""provided_value"": ""what user wrote or null"",
        ""matched_department"": ""full department name from list or null"",
        ""confidence"": 0.0 - 1.0,
        ""accepted"": true | false
    }
        },
    ""missing_fields"": [""array of required fields that are null or unmatched""]
    }
}

Final Validation Rules:
-If confidence < 0.80, set matched_department to null and accepted to false
- If confidence >= 0.80, use the matched department name and set accepted to true
- If no department mentioned, set provided_value and matched_department to null
- Add ""department"" to missing_fields if matched_department is null

Return ONLY the JSON object, no additional text or explanations.";

                }
            }
        }
       
    }
}