using Microsoft.SemanticKernel;
using NearbyCS_API.Storage.Contract;
using NearbyCS_API.Utlls;
using System.ComponentModel;
using System.Text.Json;
using System.Threading.Tasks;

public class CheckPolicyComplianceTool  
{
    public string Name => "CheckPolicyCompliance";
   
    [KernelFunction]
    [Description("Checks if a purchase request complies with company procurement policies.")]
    public async Task<string> CheckPolicyComplianceAsync(
        Kernel kernel,
        [Description("Category of the purchase request (e.g., Hardware, Software, Office Supplies)")] string category,
        [Description("Specific item being requested")] string item,
        [Description("Number of items being requested")] int quantity,
        [Description("Department making the request")] string department,
        [Description("Cost per unit of the item")] decimal unitCost)
    {
        // Prepare the prompt by replacing placeholders with actual values
        var prompt = CheckCompliancePrompt
            .Replace("{{Category}}", category)
            .Replace("{{Item}}", item)
            .Replace("{{Quantity}}", quantity.ToString())
            .Replace("{{UnitCost}}", unitCost.ToString())
            .Replace("{{Department}}", department);

        // Call the kernel to get the model's response
        var result = await kernel.InvokePromptAsync(prompt, new() {
            { "Category", category },
            { "Item", item },
            { "Quantity", quantity.ToString() },
            { "UnitCost", unitCost.ToString() },
            { "Department", department }
        });

        // Return the model's raw response (should be JSON)
        return result.ToString();
    }

    [KernelFunction]
    [Description("Checks if a purchase request complies with company procurement policies using a JSON input.")]
    public async Task<string> CheckPolicyComplianceFromJsonAsync(
        Kernel kernel,
        [Description("JSON string containing purchase request details (category, item, quantity, department, unitCost)")] string jsonInput)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonInput);
            var root = doc.RootElement;

            // Use utility class for resilient parsing with smart defaults
            var category = JsonPropertyExtractor.ExtractStringProperty(root, "category", "Other");
            var item = JsonPropertyExtractor.ExtractStringProperty(root, "item", "Unknown Item");
            var quantity = JsonPropertyExtractor.ExtractIntProperty(root, "quantity", 1);
            var department = JsonPropertyExtractor.ExtractStringProperty(root, "department", "General");
            var unitCost = JsonPropertyExtractor.ExtractDecimalProperty(root, "unitCost", 0m);

            return await CheckPolicyComplianceAsync(kernel, category, item, quantity, department, unitCost);
        }
        catch (JsonException ex)
        {
            return JsonSerializer.Serialize(new 
            { 
                error = true, 
                message = $"Invalid JSON format: {ex.Message}"
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

1. Hardware purchases for the Engineering department must not exceed $1000 per unit.
2. Hardware requests over 10 units require department head approval.
4. Laptop requests are limited to one per employee every 3 years.
5. Only pre-approved vendors may be used for laptops, desktops, and servers.
8. Any single requisition exceeding $50,000 must be routed to Finance VP for approval.
9. Recurring SaaS purchases must be reviewed annually before renewal.
10. Bulk orders over 25 units must include supplier discount verification.
11. Desktop computers are not allowed for remote-only employees.
12. Hardware upgrades must be justified by age (minimum 36-month lifecycle).
13. Any purchase tagged as ""urgent"" will trigger post-purchase audit.
14. Purchases of personal electronics (e.g., tablets, phones) must include asset tracking IDs.
15. All vendors must pass a compliance check with the Procurement Risk team prior to first use.

---REQUEST---
Category: {{Category}}
Item: {{Item}}
Quantity: {{Quantity}}
UnitCost: {{UnitCost}}
Department: {{Department}}

---

Instructions:

- For each policy listed above, check if the purchase request violates the rule.
- If a violation is found, add a brief string description to the ""violations"" array. Use one string per violated policy; be concise.
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