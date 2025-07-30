using SingleAgent.Models.DTO;

namespace SingleAgent.Storage.Contract
{
    public interface IProductRepository
    {
        Task<ProductDTO?> GetBySku(string sku);
        Task<List<ProductDTO>> GetBySkus(IEnumerable<string> skus);
        Task<List<ProductDTO>> GetAllProductsSummaryViewAsync();
        Task<List<ProductDTO>> GetAlternativeProductsAsync();
    }
}
