using Microsoft.SemanticKernel;
using NearbyCS_API.Agents;
using NearbyCS_API.Storage.Contract;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NearbyCS_API.Future.Tools
{
    public class ShowQualifiedProductsTool
    {
        private readonly ILogger<ShowQualifiedProductsTool> _logger; // Logger for this agent
        private readonly IProductRepository _productRepository;

        public ShowQualifiedProductsTool(ILogger<ShowQualifiedProductsTool> logger, IProductRepository productRepository)
        {
            _logger = logger;
            _productRepository = productRepository;
        }

        [KernelFunction]
        [Description("Returns a JSON array of all qualified products with all their details.")]
        public async Task<string> ShowAllQualifiedProductsAsync()
        {
            _logger.LogInformation("Processing request to show all qualified products");
            var products = await _productRepository.GetAllProductsSummaryViewAsync();
            if (products == null || !products.Any())
            {
                _logger.LogWarning("No products found in repository.");
                return JsonSerializer.Serialize(new { status = "error", message = "No products found." });
            }
            // Return all product fields as a JSON array
            return JsonSerializer.Serialize(products);
        }
    }
}
