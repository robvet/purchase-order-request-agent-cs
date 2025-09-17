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





 Device Return/Decommission Check
 - 
Here’s a focused strategy for the Device Return/Decommission Check step—designed for clarity, compliance, and minimal user friction:

1. Prompt User for Asset Return
After passing cost and asset age checks, agent asks:

“Will you be returning your current laptop? Please confirm.”

2. Branch on User Response
If “Yes”:

Prompt: “Please enter the serial number or asset tag of the laptop you’ll be returning.”

Log this for downstream tracking and IT coordination.

If “No”:

Prompt: “Please specify the reason:

Lost

Stolen

Damaged beyond repair

Device already returned

Other (please explain)”

Capture and log the reason.

3. Automate Business Rule Handling
If asset info is provided:

Pass details to IT asset management for pickup/decommission scheduling.

If marked lost/stolen:

Automatically flag for compliance/security follow-up (e.g., incident report).

If damaged:

Prompt for damage details (and photo, if possible); route for further review if needed.

4. Audit and Enforcement
Store all return/decommission answers with the request.

Block purchase if no return or reason is provided.

Agentic AI Extensions
Auto-fill previous asset info from inventory if available.

If user hesitates, provide reminders about company policy:
“Per IT policy, new laptops require return or documented loss/damage of your current device.”

If reason is ambiguous, escalate to human (e.g., IT asset manager) for review.

Structured Output Schema Example
json
Copy
Edit
{
  "deviceReturnStatus": "to_return" | "lost" | "stolen" | "damaged" | "already_returned" | "other",
  "assetTag": "ABC12345",
  "additionalInfo": "Screen shattered after accident.",
  "requiresFollowup": true
}

Summary Table
Scenario	    Action/Prompt	                            Next Step
Will return	    Ask for asset tag/serial number	            Log, schedule pickup
Lost/Stolen	    Ask for details; flag for compliance	    Incident process
Damaged	        Ask for explanation/photo; flag for review	IT/Asset review
Other/Unknown	Require explanation, escalate if unclear	Human follow-up


This approach ensures:

Every laptop is tracked or accounted for

Policy is enforced without excess friction

You build a clear audit/compliance trail

User gets a guided, agentic experience