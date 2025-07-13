# PurchaseOrderRequest-SingleAgent


##🧠 Final 8-Tool Set for a Next-Level Agentic PO Demo


| Tool                     | Function                                         |
|--------------------------|--------------------------------------------------|
| ClassifyRequest          | Understand the category of need                  |
| CheckBudget              | Financial guardrails                             |
| SuggestVendors           | Align with sourcing agreements                   |
| BuildRequisition         | Structure the PO                                 |
| SubmitForApproval        | Route to human decision-maker                    |
| CheckPolicyCompliance    | Block/prevent non-compliant asks                 |
| SuggestAlternatives      | Inject cost- and availability-aware options      |
| CheckInventoryOrTransfer | Enable lateral transfers before new spend        |



public class AgentState
{
    public Dictionary<string, JsonElement> ToolResults { get; set; } = new();
    public List<string> ToolCallHistory { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}


I'll explain how the tool calls work with the PurchaseOrderAgent and how to access the tool results.
When your agent makes a call to chatService.GetChatMessageContentAsync(), it passes the chat history, execution settings with ToolCallBehavior.AutoInvokeKernelFunctions, and the kernel that has your tools registered. Here's what happens behind the scenes:
1.	The LLM (like GPT-4) receives your chat history and generates a response
2.	When the LLM decides it needs to use a tool, it generates a tool call in a specific format
3.	Semantic Kernel intercepts these tool calls and automatically executes them using your registered tools
4.	The tool results are fed back to the LLM, which then incorporates them into its final response
5.	This cycle can happen multiple times in a single GetChatMessageContentAsync call
The issue you're facing is that you don't see the individual tool calls and their JSON results because they're all handled internally during this process. Let me show you how to capture and log these tool results:


How Tool Calls Work in Semantic Kernel
The Process
1.	LLM Decision to Use a Tool: When the LLM (like GPT-4) receives your prompt, it might decide it needs information from a tool. For example, if you ask about a purchase's policy compliance, it might decide to use CheckPolicyComplianceTool.
2.	Tool Call Generation: The LLM generates a function call with appropriate parameters:

{
  "name": "CheckPolicyComplianceAsync",
  "parameters": {
    "category": "Office Supplies",
    "item": "Printer Paper",
    "quantity": 10,
    "department": "Finance",
    "unitCost": 5.25
  }
}

3.	Automatic Invocation: When using ToolCallBehavior.AutoInvokeKernelFunctions, Semantic Kernel intercepts this function call, finds your registered CheckPolicyComplianceTool, and automatically calls it with the provided parameters.
4.	Tool Execution: Your tool executes and returns a JSON response:

  {
     "compliant": true,
     "violations": []
  }

5.	Response Integration: The JSON result is sent back to the LLM, which then incorporates this information in its final response.