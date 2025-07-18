using Azure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.SemanticKernel;
using NearbyCS_API.Agents;
using NearbyCS_API.Models.DTO;
using NearbyCS_API.Storage.Contract;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

[Description("Determines the category of a user's purchase request (e.g., hardware, software, services) using a language model.")]
public class ClassifyRequestTool
{
    public string Name => "ClassifyRequest";
    private readonly ILogger<PurchaseOrderAgent> _logger; // Logger for this agent
    private readonly IProductRepository _productRepository;

    public ClassifyRequestTool(ILogger<PurchaseOrderAgent> logger, IProductRepository productRepository)
    {
        _logger = logger;
        _productRepository = productRepository;
    }

    [KernelFunction]
    [Description("Returns the category and confidence score for a user’s purchase-related request in JSON format.")]
    public async Task<string> ClassifyRequestAsync(
        Kernel kernel,
        [Description("Natural language text describing what the user wants to purchase.")] string requestText)
    {
        _logger.LogInformation("Processing user request in ClassifyRequestTool: {UserPrompt}", requestText); // Log the user prompt

        var toolPrompt = PromptTemplate.ClassifyRequestPromptTempate(requestText).Replace("{{requestText}}", requestText);

        // Call the kernel to get the model's response
        var result = await kernel.InvokePromptAsync(toolPrompt, new() {
            { "requestText", requestText }
        });

        _logger.LogInformation("Output from ClassifyRequestTool: {Output}", result.ToString());

        // The model's response should be a JSON object with one of the following schemas:        

        // Parse and enrich ambiguous response
        string rawJson = result.ToString();
        try
        {
            var json = JsonNode.Parse(rawJson);
            var status = json?["status"]?.ToString();
            var userPrompt = json?["userPrompt"]?.ToString();
            var skus = json?["skus"]?.AsArray()?.Select(s => s?.ToString()).ToList() ?? new List<string>();

            List<ProductDTO> products = new List<ProductDTO>();

            if (skus == null || skus.Count == 0)
            {
                // If no SKUs are provided, return an empty list
                products = _productRepository.GetAll();
            }
            else
            {
                products = _productRepository.GetBySkus(skus);
            }
            
            // Construct your API response object
            var response = new
            {
                status,
                userPrompt,
                products
            };


            // category (unit)
            // confidenceScore (float)


            return JsonSerializer.Serialize(response); // Or however you write JSON in your API framework
        }
        catch (Exception ex)
        {
            // Return error response
            // Serialize the error as JSON string
            var error = new { error = $"Failed to parse model response: {ex.Message}" };
            return JsonSerializer.Serialize(error);
        }
    }

    private static class PromptTemplate
    {
        public static string ClassifyRequestPromptTempate(string requestText)
        {
            return @"You are an agent that identifies requested products for purchase.

Supported products (sku: name):
- MBP-16-M3: MacBook Pro 16” (M3 Pro)
- MBP-14-M3: MacBook Pro 14” (M3 Pro)
- DELL-LAT5440: Dell Latitude 5440
- DELL-XPS13: Dell XPS 13
- LEN-T14S: Lenovo ThinkPad T14s
- LEN-X1C10: Lenovo ThinkPad X1 Carbon G10
- HP-ELITE840: HP EliteBook 840 G10
- SURF-LAP-STUDIO2: Surface Laptop Studio 2
- SURF-PRO9: Surface Pro 9 Tablet
- ASUS-EXPERT: ASUS ExpertBook B9
- ACER-TMP6: Acer TravelMate P6

User Request:
{{requestText}}

Return only a valid JSON object using **one** of these schemas:

If the request matches exactly one product:
{ ""status"": ""matched"", ""skus"": [""MBP-16-M3""], ""userPrompt"": ""You requested a MacBook Pro 16”. Please confirm or specify configuration details (RAM, storage, etc.)."" }

If the request could refer to more than one product:
{ ""status"": ""ambiguous"", ""skus"": [""MBP-16-M3"", ""MBP-14-M3""], ""userPrompt"": ""Did you mean the 14” or 16” MacBook Pro? Please clarify."" }

If no product is found:
{ ""status"": ""not_found"", ""skus"": [], ""userPrompt"": ""No supported product matched your request. Please choose from the list above."" }

If you are not certain, or more than one product might match, always use the ""ambiguous"" schema.

Do NOT include any explanations, markdown, or extra text—return ONLY the JSON object.";
        }
    }

}

// Always return an array for "skus", even for single matches 



//You are an agent responsible for identifying the requested product in a purchase order.

//Supported products:
//- Surface Laptop Studio 2
//- MacBook Pro 16” (M3 Pro)
//- MacBook Pro 14” (M3 Pro)
//- Dell Latitude 5440
//- Lenovo ThinkPad T14s
//- Lenovo ThinkPad X1 Carbon G10
//- HP EliteBook 840 G10
//- Surface Pro 9 Tablet
//- Dell XPS 13
//- Acer TravelMate P6
//- ASUS ExpertBook B9

//User Request:
//{{requestText}}

//Respond only with a valid JSON object, using ONE of the following schemas:

//If the user request matches exactly one product:
//{
//  ""status"": ""matched"",
//  ""sku"": ""...""
//}

//If the request could refer to more than one product:
//{
//  ""status"": ""ambiguous"",
//  ""suggestions"": [
//    { ""sku"": ""..."", ""name"": ""..."" }
//    // ...other possible matches
//  ]
//}

//If no supported product is found:
//{
//  ""status"": ""not_found"",
//  ""message"": ""No supported product matched your request. Please choose from the list.""
//}

//Do NOT include explanations, markdown, reflections, commentary, or any extra text—return ONLY a valid JSON object, with no other output.
//




// Case-insensitive substring match: Finds any product if any word in the query matches any part of product name, description, or SKU.
// Splits input string: Checks each keyword separately(so “surface laptop” will find “Surface Laptop Studio 2” even if order / spacing differs).
// Fallback: If no match, return “no match found” or list all products for user help.


// old prompt
//Classify the following user request into one of the following categories:
//-Hardware
//- Software
//- Services
//- Travel
//- Furniture
//- Other

//You must respond ONLY with a valid JSON object in the following format. Do not include any additional text, explanations, or formatting:
//{
//    """"category"""": """"<category>"""",
//  """"confidenceScore"""": < float between 0 and 1 >
//}

//Example response:
//{
//    """"category"""": """"Hardware"""",
//  """"confidenceScore"""": 0.95
//}

//User Request:
//{ { requestText} }



//private static class PromptTemplate
//{
//    public static string ClassifyRequestPromptTempate(string requestText)
//    {
//        return @"You are an agent responsible for identifying products in purchase requests.

//You are an agent responsible for identifying products in purchase requests.

//Supported products:

//- MBP-16-M3: MacBook Pro 16” (M3 Pro): Apple 16-inch, M3 Pro, 32GB RAM, 1TB SSD
//- MBP-14-M3: MacBook Pro 14” (M3 Pro): Apple 14-inch, M3 Pro, 32GB RAM, 1TB SSD
//- DELL-LAT5440: Dell Latitude 5440: 14” i7, 32GB RAM, 1TB SSD, business laptop
//- DELL-XPS13: Dell XPS 13: 13.4” UHD+, i7, 32GB RAM, 1TB SSD, touch
//- LEN-T14S: Lenovo ThinkPad T14s: 14” Ryzen 7, 32GB RAM, 1TB SSD, enterprise build
//- LEN-X1C10: Lenovo ThinkPad X1 Carbon G10: 14” i7, 32GB RAM, 1TB SSD, ultralight premium
//- HP-ELITE840: HP EliteBook 840 G10: 14” i5, 32GB RAM, 1TB SSD, ultrabook
//- SURF-LAP-STUDIO2: Surface Laptop Studio 2: 14.4” PixelSense touchscreen, Intel i7, 32GB RAM, 1TB SSD, NVIDIA RTX
//- SURF-PRO9: Surface Pro 9 Tablet: 13” tablet, i7, 32GB RAM, 1TB SSD, detachable keyboard
//- ASUS-EXPERT: ASUS ExpertBook B9: 14” i7, 32GB RAM, 1TB SSD, long battery life
//- ACER-TMP6: Acer TravelMate P6: 14” FHD IPS, Intel i7, 32GB RAM, 1TB SSD, ultra-light business laptop

//User Request:
//{{requestText}}

//If the user's request matches more than one supported product (for example, both MacBook Pro 14” and 16”), return all possible matching products in the suggestions array using the “ambiguous” schema. 
//Never select just one product if multiple plausible matches exist. 
//If there is any uncertainty, always return all matching options.

//If the user request matches exactly one product:
//{
//  ""status"": ""matched"",
//  ""sku"": ""...""
//}

//If the request could refer to more than one product:
//{
//  ""status"": ""ambiguous"",
//  ""suggestions"": [
//    { ""sku"": ""..."", ""name"": ""..."" }
//    // ...other possible matches
//  ]
//}

//{
//  ""status"": ""not_found"",
//  ""message"": ""No supported product matched your request. Please choose from the list."",
//  ""suggestions"": [
//    { ""sku"": ""..."", ""name"": ""..."" }
//    // ...all available products
//  ]
//}

//Do NOT include explanations, markdown, reflections, commentary, or any extra text—return ONLY a valid JSON object, with no other output.

//            After you determine the result(matched, ambiguous, or not_found), return only a valid JSON object with these three fields:

//            -reflection: (Briefly explain your reasoning based on the product match result.)
//-nextStep: (State the next action, such as ""Ask the requester to clarify which product"" or ""Proceed to budget check."")
//-userPrompt: (The exact question or instructions to display to the user.)

//Example:
//            {
//                ""reflection"": ""The classification tool identified the request as ambiguous, offering two possible MacBook Pro models..."",
//  ""nextStep"": ""Pause to ask the requester for clarification."",
//  ""userPrompt"": ""Do you want the 14"" or 16"" MacBook Pro? Any specific RAM, storage, or processor requirements?""
//}

//            Do NOT include any text outside the JSON object.";

//    }