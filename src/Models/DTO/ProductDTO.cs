namespace NearbyCS_API.Models.DTO
{
    public class ProductDTO
    {
        public string Sku { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinPrice { get; set; }
        public int MaxPrice { get; set; }
        public string ImageUrl { get; set; }
    }
}
