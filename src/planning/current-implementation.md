# Current Implementation Plan for Procurement Agent

## Agent Toolflow (Best Practice)
 - IntentRouterTool: Examples: “RequestPurchase”, “ShowSupportedModels”, “ShowSpecs”, “Help”, etc.

    <input>
       userPromptInput
    </input>

   <output>
     Json
     {
       intent,
       confidence,
       userRequest
     };
   </output




   Current State Management Analysis:
Looking at the IStateStore interface, it currently only manages:
•	ChatHistory (conversation messages)
•	But NO structured purchase request state
The Core Problem:
When the user says "Finance", the agent has access to the conversation history but no structured state about:
•	What product they originally requested (MacBook Pro)
•	What stage of the workflow they're in
•	What information is still missing
Suggested Solutions (for your approval):
Option 1: Extend IStateStore Interface (Recommended)
Add structured request state management to IStateStore:

public interface IStateStore
{
    Task<ChatHistory?> GetChatHistoryAsync(string sessionId);
    Task SaveChatHistoryAsync(string sessionId, ChatHistory history);
    Task DeleteChatHistoryAsync(string sessionId);
    
    // NEW: Purchase request state management
    Task<PurchaseRequestState?> GetRequestStateAsync(string sessionId);
    Task SaveRequestStateAsync(string sessionId, PurchaseRequestState state);
    Task DeleteRequestStateAsync(string sessionId);
}

public class PurchaseRequestState
{
    public string? Intent { get; set; }
    public string? RequestedModel { get; set; }
    public List<string>? MatchedSkus { get; set; }
    public int? Quantity { get; set; }
    public string? Department { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Status { get; set; } // "extracting", "policy_check", "compliant", etc.
    public Dictionary<string, object>? AdditionalData { get; set; }
}



NO, absolutely not! Purchase request state should NOT be stored in chat history. Here's why this is a critical architectural distinction:
CHAT HISTORY vs PURCHASE REQUEST STATE:
ChatHistory = Conversational Memory
•	Contains the actual conversation messages (user input, agent responses)
•	Used by the LLM to understand context and generate responses
•	Tracks what was said, when, and by whom
•	Should remain clean for AI reasoning
PurchaseRequestState = Business Workflow State
•	Contains structured business data (SKUs, quantities, compliance status, etc.)
•	Tracks the actual purchase request as it moves through workflow stages
•	Persists business logic state between interactions
•	Used for business rules, compliance, and data processing
WHY SEPARATION IS CRITICAL:
1.	Data Integrity: Chat history can get long and messy. Business state needs to be clean and structured.
2.	Performance: LLMs process every message in chat history. You don't want business data bloating the context.
3.	Business Logic: Tools need reliable access to current request state without parsing conversational text.
4.	Compliance & Audit: Purchase request data needs to be stored in a structured format for reporting and compliance.
THE PROBLEM YOU DESCRIBED:
When compliance says "add department code" and user responds with "IT Department":
•	Chat History captures: "Please add department code" → "IT Department"
•	Purchase Request State should capture: requestState.Department = "IT"
Currently, the agent only saves chat history but never saves the business state, so when the user adds the department, there's no purchase request to attach it to!
This is exactly why you created the PurchaseRequestState class and extended IStateStore - to solve this architectural problem.
The agent needs both:
•	Chat history for conversational context
•	Purchase request state for business workflow continuity
Does this clarify why they need to be separate?



 - 

 - ExtractHardwareDetailsTool: Examples: Get model/SKU, quantity, upgrades, etc.

 - ClassifyRequestTool: Examples: Categorize for policy/business rules.

 - CheckPolicyComplianceTool: Examples: Is it allowed? Any warnings?

 - SuggestUpgradesTool: Examples: Addtional RAM, storage, etc.
	-  Please confirm or specify configuration details (RAM, storage, etc.).""
	- 
 - CheckInventoryTool: Examples: Already in stock?

 - SuggestAlternativesTool: Examples: Offer better/faster/cheaper options.

 - QuotePriceTool: Examples: Total cost. 

 - ApprovePurchaseTool: Examples: Confirm purchase after approval.



## Low-Hanging, Fast Tools (<1 hr each)
 - ShowSupportedModels	Return list of all supported SKUs/models
 - ShowSpecs	Return specs for any SKU
 - ShowPolicySummary	Output a 1-paragraph summary of procurement policy
 - HelpTool	List all agent intents (“what you can do here”)
 - QuotePrice	Give price for model/upgrades
 - SaveDraftRequest	Save current progress for later