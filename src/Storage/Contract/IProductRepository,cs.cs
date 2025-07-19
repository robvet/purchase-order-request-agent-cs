using NearbyCS_API.Models.DTO;

namespace NearbyCS_API.Storage.Contract
{
    public interface IProductRepository
    {
        Task<ProductDTO?> GetBySku(string sku);
        Task<List<ProductDTO>> GetBySkus(IEnumerable<string> skus);
        Task<List<ProductDTO>> GetAllProductsSummaryViewAsync();
        Task<List<ProductDTO>> GetAlternativeProductsAsync();
    }
}
