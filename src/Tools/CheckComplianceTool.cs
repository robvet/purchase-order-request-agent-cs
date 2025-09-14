using Microsoft.SemanticKernel;
using SingleAgent.Utlls;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

[Description("Validates purchase requests against procurement policies, identifying violations of cost limits, quantity restrictions, and approval requirements.")]
public class CheckComplianceTool
{
    private const string ToolName = "CheckComplianceTool";
    private readonly ILogger<CheckComplianceTool> _logger;

    public CheckComplianceTool(ILogger<CheckComplianceTool> logger)
    {
        _logger = logger;
    }

    [KernelFunction]
    [Description("Checks if a purchase request complies with company procurement policies.")]
    public async Task<string> CheckComplianceAsync(
        Kernel kernel,
        [Description("Category of the purchase request (e.g., Hardware, Software)")] string category,
        [Description("Product SKU being requested")] string sku,
        [Description("Number of items requested")] int quantity,
        [Description("Cost per unit")] decimal unitCost,
        [Description("(REQUIRED) Model's explanation for compliance check")] string reasoning,
        [Description("Department making the request")] string department = "unknown")
    {
        try
        {
            _logger.LogInformation("Processing category in {ToolName}: {category}", ToolName, category);
            _logger.LogInformation("Processing SKU in {ToolName}: {sku}", ToolName, sku);
            _logger.LogInformation("Processing quantity in {ToolName}: {quantity}", ToolName, quantity);
            _logger.LogInformation("Processing unit cost in {ToolName}: {unitCost}", ToolName, unitCost);
            _logger.LogInformation("Processing reasoning in {ToolName}: {reasoning}", ToolName, reasoning);
            _logger.LogInformation("Processing department in {ToolName}: {department}", ToolName, department);

            // validate parameters  
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(sku) ||
                quantity <= 0 || unitCost <= 0 || string.IsNullOrWhiteSpace(reasoning))
            {
                return JsonSerializer.Serialize(new
                {
                    toolname = ToolName,
                    status = "error",
                    error = "invalid_parameters",
                    message = "All parameters except department are required and must be valid",
                    category = category,
                    sku = sku,
                    quantity = quantity,
                    unitCost = unitCost,
                    department = department,
                    reasoning = reasoning,
                    timestamp = DateTime.UtcNow
                });
            }

            string prompt = CheckCompliancePrompt
                .Replace("{{Category}}", category)
                .Replace("{{sku}}", sku)
                .Replace("{{Quantity}}", quantity.ToString())
                .Replace("{{UnitCost}}", unitCost.ToString("C"))
                .Replace("{{Department}}", department);

            var result = await kernel.InvokePromptAsync(prompt, new()
            {
                ["Category"] = category,
                ["Sku"] = sku,
                ["Quantity"] = quantity.ToString(),
                ["UnitCost"] = unitCost.ToString("C"),
                ["Department"] = department
            });

            var json = JsonNode.Parse(result.ToString())
                ?? throw new JsonException("Model returned null or invalid JSON response");

            var compliant = json["compliant"]?.GetValue<bool>() ?? false;
            var violations = json["violations"]?.AsArray()?.Select(v => v?.ToString()).ToList() ?? new List<string>();

            var response = new
            {
                toolname = ToolName,
                status = compliant ? "compliant" : "non_compliant",
                violations = violations,
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
                toolname = ToolName,
                status = "error",
                error = "compliance_check_failed",
                message = $"Failed to check compliance: {ex.Message}",
                reasoning,
                timestamp = DateTime.UtcNow
            });
        }
    }

    // Prompt template remains the same

    #region Prompt Templates

    /// <summary>
    /// Prompt template for policy compliance checking with procurement rules and expected JSON response format
    /// </summary>
    private const string CheckCompliancePrompt = @"
You are a compliance reasoning agent responsible for determining whether a purchase request follows company procurement policies.

### Procurement Policy:
1. Hardware purchases must not exceed $1000 per unit
2. Hardware requests over 10 units require department head approval
3. Laptop requests are limited to one per employee every 3 years
4. Desktop computers are not allowed for employees
5. Hardware upgrades must be justified by age (36-month lifecycle)
6. Total requisition exceeding $50,000 requires Finance VP approval
7. Bulk orders over 25 units require supplier discount verification
8. Any purchase tagged as 'urgent' triggers post-purchase audit

---REQUEST---
Category: {{Category}}
Sku: {{sku}}
Quantity: {{Quantity}}
UnitCost: {{UnitCost}}
Department: {{Department}}

---

Instructions:
- Check each policy against this request
- For each violation, add a brief description to the 'violations' array
- For Policy #1 violations, use EXACTLY this message: 'This item exceeds the $1000 limit. Please provide justification for more powerful hardware.'
- Calculate total cost (Quantity * UnitCost) for Policy #6
- If no violations found, return empty violations array
- Set 'compliant' to true only if NO violations exist

Examples:
Request: Hardware request, 30 laptops at $850 each
{
    ""compliant"": false,
    ""violations"": [
        ""Hardware requests over 10 units require department head approval"",
        ""Bulk orders over 25 units require supplier discount verification""
    ]
}

Request: Hardware request, 1 laptop at $1200
{
    ""compliant"": false,
    ""violations"": [
        ""This item exceeds the $1000 limit. Please provide justification for more powerful hardware.""
    ]
}

Request: Hardware request, 1 laptop at $950
{
    ""compliant"": true,
    ""violations"": []
}

Return STRICTLY valid JSON with this structure:
{
    ""compliant"": <true|false>,
    ""violations"": [
        ""<violation description>""
    ]
}

Do NOT include any additional text, explanations, or commentary—return ONLY the JSON object.";

    #endregion
}




//private const string ToolName = "CheckComplianceTool";

//    [KernelFunction]
//[Description("Checks if a purchase request complies with company procurement policies.")]

//public async Task<string> CheckComplianceAsync(
//        Kernel kernel,
//        [Description("Category of the purchase request (e.g., Hardware, Software, Office Supplies)")] string category,
//        [Description("Specific item being requested")] string sku,
//        [Description("Number of items being requested")] int quantity,
//        [Description("Cost per unit of the item")] decimal unitCost,
//        [Description("(REQUIRED) Model's explanation for why check compliance is needed at this step")] string reasoning,
//        [Description("Department making the request (can be 'unknown' if not provided)")] string department = "unknown")
//{
//    try
//    {
//        // Validate parameters
//        if (string.IsNullOrWhiteSpace(category))
//            throw new ArgumentException("Category is required in {ToolName}.", nameof(category));
//        if (string.IsNullOrWhiteSpace(sku))
//            throw new ArgumentException("SKU is required in {ToolName}.", nameof(sku));
//        if (quantity <= 0)
//            throw new ArgumentException("Quantity must be positive in {ToolName}.", nameof(quantity));
//        if (unitCost <= 0)
//            throw new ArgumentException("Unit cost must be positive in {ToolName}.", nameof(unitCost));
//        if (string.IsNullOrWhiteSpace(reasoning))
//            throw new ArgumentException("Reasoning required in {ToolName} for audit trail and tool selection improvement.", ToolName);
//        if (string.IsNullOrWhiteSpace(department))
//            throw new ArgumentException("Department is required in {ToolName}.", nameof(department));


//        // Prepare the prompt by replacing placeholders with actual values
//        var prompt = CheckCompliancePrompt
//            .Replace("{{Category}}", category)
//            .Replace("{{sku}}", sku)
//            .Replace("{{Quantity}}", quantity.ToString())
//            .Replace("{{UnitCost}}", unitCost.ToString("C"))
//            .Replace("{{Department}}", department);

//        // Call the kernel to get the model's response
//        var result = await kernel.InvokePromptAsync(prompt, new() {
//                { "Category", category },
//                { "Sku", sku },
//                { "Quantity", quantity.ToString() },
//                { "UnitCost", unitCost.ToString("C") },
//                { "Department", department },
//                { "Reasoning", reasoning }
//            });

//        // Parse the response to ensure it matches the expected format
//        string rawJson = result.ToString();

//        try
//        {
//            using var doc = JsonDocument.Parse(rawJson);
//            var root = doc.RootElement;

//            // Validate that the response has the expected structure
//            if (!root.TryGetProperty("compliant", out _) || !root.TryGetProperty("violations", out _))
//            {
//                throw new JsonException("Response missing required 'compliant' or 'violations' properties");
//            }

//            // Return the validated JSON
//            return rawJson;
//        }
//        catch (JsonException)
//        {
//            // If parsing fails, return a structured error response
//            var fallbackResponse = new
//            {
//                ToolName = ToolName,
//                compliant = false,
//                violations = new[] { "Unable to parse policy compliance response from LLM" },
//                error = "json_parse_error"
//            };

//            return JsonSerializer.Serialize(fallbackResponse);
//        }
//    }
//    catch (Exception ex)
//    {
//        // Return error response in the expected format
//        var errorResponse = new
//        {
//            ToolName = ToolName,
//            compliant = false,
//            violations = new[] { $"Policy compliance check failed: {ex.Message}" },
//            error = "compliance_check_error"
//        };

//        return JsonSerializer.Serialize(errorResponse);
//    }
//}

//[KernelFunction]
//[Description("Checks if a purchase request complies with company procurement policies using a JSON input.")]
//public async Task<string> CheckComplianceFromJsonAsync(
//    Kernel kernel,
//    [Description("Validates purchase requests against procurement policies, checking cost limits, quantity restrictions, and approval requirements. Returns JSON with compliance status and any violations.")]
//        string jsonInput)
//{
//    try
//    {
//        using var doc = JsonDocument.Parse(jsonInput);
//        var root = doc.RootElement;

//        // Use utility class for resilient parsing with smart defaults
//        var category = JsonPropertyExtractor.ExtractStringProperty(root, "category", "Other");
//        var sku = JsonPropertyExtractor.ExtractStringProperty(root, "sku", "Unknown sku");
//        var quantity = JsonPropertyExtractor.ExtractIntProperty(root, "quantity", 1);
//        var department = JsonPropertyExtractor.ExtractStringProperty(root, "department", "General");
//        var unitCost = JsonPropertyExtractor.ExtractDecimalProperty(root, "unitCost", 0m);

//        return await CheckComplianceAsync(kernel, category, sku, quantity, unitCost, department);
//    }
//    catch (JsonException ex)
//    {
//        return JsonSerializer.Serialize(new
//        {
//            ToolName = ToolName,
//            compliant = false,
//            violations = new[] { $"Invalid JSON format: {ex.Message}" },
//            error = "json_parse_error"
//        });
//    }
//}