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
