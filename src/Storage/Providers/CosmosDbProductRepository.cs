using NearbyCS_API.Models.DTO;
using NearbyCS_API.Storage.Contract;

namespace NearbyCS_API.Storage.Providers
{
    public class CosmosDbProductRepository : IProductRepository
    {
        // Inject Cosmos client, connection, etc.

        public ProductDTO GetBySku(string sku)
        {
            // Query Cosmos DB for single SKU
            return null;
        }

        public List<ProductDTO> GetBySkus(IEnumerable<string> skus)
        {
            // Query Cosmos DB for multiple SKUs
            return null;
        }

        public List<ProductDTO> GetAll()
        {
            // Query Cosmos DB for all products
            return null;
        }
    }
}
