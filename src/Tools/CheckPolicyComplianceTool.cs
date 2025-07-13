using Microsoft.SemanticKernel;
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
3. Software licenses must be tied to an active employee ID.
4. Travel booked within 14 days of departure must include justification for urgency.
5. Laptop requests are limited to one per employee every 3 years.
6. Only pre-approved vendors may be used for laptops, desktops, and servers.
7. Furniture over $2500 per item must be justified with a workspace expansion plan.
8. Marketing software over $5000 requires legal review for licensing terms.
9. Cloud subscriptions exceeding $2000/month must include termination clauses.
10. Any single requisition exceeding $50,000 must be routed to Finance VP for approval.
11. Training requests over $1500 per employee must include ROI justification.
12. Recurring SaaS purchases must be reviewed annually before renewal.
13. Headphones and peripherals are capped at $300 per person annually.
14. Consultants must be contracted through approved service vendors only.
15. Bulk orders over 25 units must include supplier discount verification.
16. Desktop computers are not allowed for remote-only employees.
17. All software requests must specify the number of licenses and renewal terms.
18. Hardware upgrades must be justified by age (minimum 36-month lifecycle).
19. Video conferencing equipment over $5000 must include IT architecture sign-off.
20. Office chairs must be selected from the ergonomic list provided by Facilities.
21. International travel requires Director-level approval at minimum.
22. Departments may not exceed their quarterly discretionary budget limits.
23. Any purchase tagged as ""urgent"" will trigger post-purchase audit.
24. Purchases of personal electronics (e.g., tablets, phones) must include asset tracking IDs.
25. All vendors must pass a compliance check with the Procurement Risk team prior to first use.

---

## Request

Category: {{Category}}  
Item: {{Item}}  
Quantity: {{Quantity}}  
UnitCost: {{UnitCost}}  
Department: {{Department}} 

---

### Instructions:

- Analyze the request against all applicable policies.
- Determine whether the request complies with all rules.
- Return the result using the following JSON structure:
{
  ""compliant"": <true|false>,
  ""violations"": [
    ""<explanation of each policy violated>""
  ]
}
";

    #endregion
}