# Agent Developer Notes

### Best Practices for Agent Development

 - Tools should return data as a structured response, i.e., JSON, or a strongly typed object.
		
	- 1. Improves the deterministic nature of the agent's responses.
	- 2. Allows for easier parsing and manipulation of data.
	- 3. Enables better error handling and validation.
	- 4. Provides a consistent format for data exchange between tools and the agent.
	- 5. Facilitates integration with other systems and services.
	- 6. Improves the overall maintainability and readability of the code.
	- 7. Allows for better debugging and testing of the agent's functionality.
	- 8. Supports extensibility, allowing new tools to be added without breaking existing functionality.
	- 9. Enhances the agent's ability to reason about the data and make informed decisions based on it.

 - If tools return a JSON structured response, do we need specific model files to capture the value for each structured response?
	
	- No, you don't need specific model files for each structured response. The agent can dynamically parse the JSON responses and 
	  extract the necessary values at runtime. This approach allows for greater flexibility and reduces the need for maintaining multiple model files.
 	- The agent can use the tool results in its reasoning process. This means that the agent can make decisions based on the data returned by the tools, 
	  leading to more accurate and relevant responses.
	
	- Under the hood, the agent becomes aware of tool response schemas through multiple mechanisms:
	
		- **Tool Registration**: When tools are registered, their expected input and output schemas can be defined. This allows the agent to understand what data to expect.**
		- **Embedded Schema Examples from the prompts shots example**
		- **System Prompt Hints**
		- **Runtime Learning Through Tool Calls Results**: As the agent interacts with tools, it learns about the structure of the responses. If a tool returns a JSON object, the agent can infer the schema from the response.
		- **Dynamic Parsing**: The agent can parse JSON responses at runtime, extracting relevant fields based on the tool's schema.










Those values are slattered and buried in several different json elements. How much value would there be to pull those value out in the agent after the LM calls, add them to a single json element called PurchaseHistoryState to make the value easy to find for the LM for future requests?



That's an excellent question, and you've hit on the exact reason why the ReconstructStateFromHistory method is so important. You are absolutely right—the values are scattered across different JSON objects from different tool calls, buried in the chat history.
Pulling them out and consolidating them into a single, clean PurchaseRequestState object before the next LLM call provides immense value. It is arguably one of the most critical patterns for building a robust and reliable agent.
Here’s why that "extra step" is so beneficial:
1.	Clarity and Focus for the LLM: Instead of forcing the language model to re-read and re-interpret the entire chat history on every turn, you are giving it a clean, simple summary of the current business state. It's the difference between giving a manager a one-page executive summary versus a 50-page transcript of every meeting. The model can immediately see "Status: awaiting_justification" and know what to do next, without getting distracted by earlier conversational turns.
2.	Reduces Prompt Size and Cost: Chat histories can become very long. Sending the full, verbose history with every request consumes more tokens, which increases cost and latency. The PurchaseRequestState is a highly compressed, token-efficient summary of everything the agent needs to know to continue the workflow.
3.	Increases Reliability and Reduces Errors: Relying on an LLM to correctly parse a long, complex chat history every single time is brittle. It might miss a detail or get confused by the back-and-forth. By programmatically reconstructing the state in C#, you create a deterministic and reliable view of the world. Your C# code acts as a definitive "state reducer," which is far more trustworthy than repeated LLM interpretation.
4.	Easier Debugging: When the agent makes a mistake, it's much easier for you, the developer, to look at the clean PurchaseRequestState object that was passed to the prompt and understand why. The log message _logger.LogInformation("Reconstructed State: ...") is a perfect example of this—it gives you a crystal-clear snapshot of the state that the agent was working with.
In short, the pattern you're describing is a best practice. The ReconstructStateFromHistory method ensures the agent doesn't just have a memory of the conversation, but a clear understanding of the state of the business process.


Of course. The best way to extract and consolidate the values is to create a dedicated method that processes the chat history. In your workspace, this is already implemented perfectly in the ReconstructStateFromHistory method within the PurchaseOrderAgent.cs file.
Here is a breakdown of how that specific method works, which serves as an excellent suggestion for an implementation pattern:
The Extraction Strategy: ReconstructStateFromHistory
The core idea is to treat the chat history as a log of events. The method iterates through this log and builds the current state object based on the tool outputs it finds.
Here’s the step-by-step logic from your code:
1.	Create a Blank State: It starts by initializing a new, empty PurchaseRequestState object. This will be the consolidated object that gets populated.

```csharp
    var state = new PurchaseRequestState
    {
        AdditionalData = new Dictionary<string, object>()
    };
```

2.	Isolate Tool Messages: It filters the entire chat history to get only the messages that were generated by a tool. These are the messages that contain the structured JSON responses.
```csharp
    var toolMessages = chatHistory.Where(m => m.Role == AuthorRole.Tool).ToList();
```
3.	Loop and Parse: It iterates through each tool message. Inside the loop, it parses the JSON string content of the message into a JsonNode object, which makes it easy to query.

    foreach (var toolMessage in toolMessages)
    {
        // ...
        var toolResult = JsonNode.Parse(toolMessage.Content);
        // ...
    }

4.	Identify and Extract: This is the crucial part. The code uses a series of if statements to check for the existence of unique keys within the JSON. This is how it identifies which tool produced the result.
•	If it finds an "intent" key, it knows the result is from ClassifyIntentTool and updates the state.Intent and state.Status.
•	If it finds an "is_workplace_computer" key, it knows the result is from ValidateProductTool and updates the status accordingly.
•	If it finds a "sku" key, it assumes the result is from ExtractDetailsTool and populates state.MatchedSkus, state.Quantity, etc							.
•	This continues for "compliant" from the CheckComplianceTool and "justification_approved" from the JustifyApprovalTool.
```csharp
    // Example for the ClassifyIntentTool result
    if (toolResult["intent"] != null)
    {
        state.Intent = toolResult["intent"]?.ToString();
        state.AdditionalData["lastCompletedTool"] = "ClassifyIntentTool";
        state.Status = "classified";
    }

    // Example for the CheckComplianceTool result
    if (toolResult["compliant"] != null)
    {
        state.AdditionalData["lastCompletedTool"] = "CheckComplianceTool";
        state.Status = toolResult["compliant"]?.GetValue<bool>() == true ? "compliant" : "awaiting_justification";
    }
```
 
```

This pattern is highly effective because it's both simple and robust. It centralizes all the state logic in one place and creates a clean, consolidated PurchaseRequestState object that makes the agent's job much easier in the subsequent steps.