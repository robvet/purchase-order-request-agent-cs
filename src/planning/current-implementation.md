# Current Implementation Plan for Procurement Agent

## Agent Toolflow (Best Practice)
 - IntentRouterTool: Examples: “RequestPurchase”, “ShowSupportedModels”, “ShowSpecs”, “Help”, etc.

 - ExtractOrderDetailsTool: Examples: Get model/SKU, quantity, upgrades, etc.

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