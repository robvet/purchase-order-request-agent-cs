using NearbyCS_API.Models.DTO;

namespace NearbyCS_API.Storage.Contract
{
    public interface IProductRepository
    {
        ProductDTO GetBySku(string sku);
        List<ProductDTO> GetBySkus(IEnumerable<string> skus);
        List<ProductDTO> GetAll();
    }
}
