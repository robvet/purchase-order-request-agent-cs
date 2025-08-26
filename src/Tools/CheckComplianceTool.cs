using Microsoft.SemanticKernel;
using SingleAgent.Utlls;
using System.ComponentModel;
using System.Text.Json;

public class CheckComplianceTool  
{
    public string Name => "CheckPolicyCompliance";
   
    [KernelFunction]
    [Description("Checks if a purchase request complies with company procurement policies.")]
    public async Task<string> CheckComplianceAsync(
        Kernel kernel,
        [Description("Category of the purchase request (e.g., Hardware, Software, Office Supplies)")] string category,
        [Description("Specific item being requested")] string sku,
        [Description("Number of items being requested")] int quantity,
        [Description("Cost per unit of the item")] decimal unitCost,
        [Description("Department making the request (can be 'unknown' if not provided)")] string department = "unknown")
    {
        try
        {
            // Prepare the prompt by replacing placeholders with actual values
            var prompt = CheckCompliancePrompt
                .Replace("{{Category}}", category)
                .Replace("{{sku}}", sku)
                .Replace("{{Quantity}}", quantity.ToString())
                .Replace("{{UnitCost}}", unitCost.ToString("C"))
                .Replace("{{Department}}", department);

            // Call the kernel to get the model's response
            var result = await kernel.InvokePromptAsync(prompt, new() {
                { "Category", category },
                { "Sku", sku },
                { "Quantity", quantity.ToString() },
                { "UnitCost", unitCost.ToString("C") },
                { "Department", department }
            });

            // Parse the response to ensure it matches the expected format
            string rawJson = result.ToString();
            
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;
                
                // Validate that the response has the expected structure
                if (!root.TryGetProperty("compliant", out _) || !root.TryGetProperty("violations", out _))
                {
                    throw new JsonException("Response missing required 'compliant' or 'violations' properties");
                }
                
                // Return the validated JSON
                return rawJson;
            }
            catch (JsonException)
            {
                // If parsing fails, return a structured error response
                var fallbackResponse = new
                {
                    compliant = false,
                    violations = new[] { "Unable to parse policy compliance response from LLM" },
                    error = "json_parse_error"
                };
                
                return JsonSerializer.Serialize(fallbackResponse);
            }
        }
        catch (Exception ex)
        {
            // Return error response in the expected format
            var errorResponse = new
            {
                compliant = false,
                violations = new[] { $"Policy compliance check failed: {ex.Message}" },
                error = "compliance_check_error"
            };
            
            return JsonSerializer.Serialize(errorResponse);
        }
    }

    [KernelFunction]
    [Description("Checks if a purchase request complies with company procurement policies using a JSON input.")]
    public async Task<string> CheckComplianceFromJsonAsync(
        Kernel kernel,
        [Description("JSON string containing purchase request details (category, item, quantity, department, unitCost)")] string jsonInput)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonInput);
            var root = doc.RootElement;

            // Use utility class for resilient parsing with smart defaults
            var category = JsonPropertyExtractor.ExtractStringProperty(root, "category", "Other");
            var sku = JsonPropertyExtractor.ExtractStringProperty(root, "sku", "Unknown sku");
            var quantity = JsonPropertyExtractor.ExtractIntProperty(root, "quantity", 1);
            var department = JsonPropertyExtractor.ExtractStringProperty(root, "department", "General");
            var unitCost = JsonPropertyExtractor.ExtractDecimalProperty(root, "unitCost", 0m);

            return await CheckComplianceAsync(kernel, category, sku, quantity, unitCost, department);
        }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(new 
            { 
                compliant = false,
                violations = new[] { $"Invalid JSON format: {ex.Message}" },
                error = "json_parse_error"
            });
        }
    }

    #region Prompt Templates

    /// <summary>
    /// Prompt template for policy compliance checking with procurement rules and expected JSON response format
    /// </summary>
    public const string CheckCompliancePrompt = @"
You are a compliance reasoning agent responsible for determining whether a purchase request follows company procurement policies.

### Procurement Policy:

1. Hardware purchases must not exceed $1000 per unit.
2. Hardware requests over 10 units require department head approval.
4. Laptop requests are limited to one per employee every 3 years.
5. Desktop computers are not allowed for employees.
6. Hardware upgrades must be justified by age (minimum 36-month lifecycle).
7. Only pre-approved vendors may be used for laptops, desktops, and servers.
8. Any single requisition exceeding $50,000 must be routed to Finance VP for approval.
9. Bulk orders over 25 units must include supplier discount verification.
10. Desktop computers are not allowed for employees.
11. Hardware upgrades must be justified by age (minimum 36-month lifecycle).
13. Any purchase tagged as ""urgent"" will trigger post-purchase audit.


---REQUEST---
Category: {{Category}}
Sku: {{sku}}
Quantity: {{Quantity}}
UnitCost: {{UnitCost}}
Department: {{Department}}

---

Instructions:

- For each policy listed above, check if the purchase request violates the rule.
- If a violation is found, add a brief string description to the ""violations"" array. Use one string per violated policy; be concise.
- Special case: If the violation is for exceeding cost limits (Policy #1), use this exact message: ""This item exceeds the $1000 limit. Please provide justification for more powerful hardware.""
- If no policies are violated, leave the ""violations"" array empty.
- Set ""compliant"" to true if there are no violations; otherwise, set it to false.
- Return ONLY a valid JSON object using this exact structure:

{
  ""compliant"": <true|false>,
  ""violations"": [
    ""<short description of policy violation, if any>""
  ]
}

Do NOT include any additional text, explanations, or commentary—return ONLY the JSON object.

";

    #endregion
}

