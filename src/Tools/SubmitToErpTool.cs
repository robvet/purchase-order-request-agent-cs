using System.Text.Json;
using System.Threading.Tasks;

public class SubmitToERPTool 
{
    public string Name => "SubmitToERP";
    public Task<string> InvokeAsync(string input)
    {
        var result = new { status = "submitted", invoiceId = "INV-92834" };
        return Task.FromResult(JsonSerializer.Serialize(result));
    }
}