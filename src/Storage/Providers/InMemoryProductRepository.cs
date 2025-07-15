using NearbyCS_API.Models.DTO;
using NearbyCS_API.Storage.Contract;

namespace NearbyCS_API.Storage.Providers
{
    public class InMemoryProductRepository : IProductRepository
    {
        private static readonly List<ProductDTO> _products;

        static InMemoryProductRepository()
        {
            _products = new List<ProductDTO>
            {
                new ProductDTO {
                    Sku = "MBP-16-M3",
                    Name = "MacBook Pro 16” (M3 Pro)",
                    Description = "Apple 16-inch, M3 Pro, 32GB RAM, 1TB SSD",
                    MinPrice = 3099,
                    MaxPrice = 3499,
                    ImageUrl = "https://www.apple.com/v/macbook-pro-16/h/images/overview/hero_macbookpro__fqtk8pe4h76y_large.jpg"
                },
                new ProductDTO {
                    Sku = "MBP-14-M3",
                    Name = "MacBook Pro 14” (M3 Pro)",
                    Description = "Apple 14-inch, M3 Pro, 32GB RAM, 1TB SSD",
                    MinPrice = 2699,
                    MaxPrice = 3099,
                    ImageUrl = "https://store.storeimages.cdn-apple.com/4982/as-images.apple.com/is/mbp14-spaceblack-gallery1-202310?wid=2000&hei=1536&fmt=jpeg&qlt=95&.v=1697312056758"
                },
                new ProductDTO {
                    Sku = "DELL-LAT5440",
                    Name = "Dell Latitude 5440",
                    Description = "14” i7, 32GB RAM, 1TB SSD, business laptop",
                    MinPrice = 1699,
                    MaxPrice = 2099,
                    ImageUrl = "https://i.dell.com/sites/imagecontent/products/PublishingImages/latitude-14-5440-laptop/spi/ng/laptop-latitude-14-5440-hero-500-ng.psd"
                },
                new ProductDTO {
                    Sku = "DELL-XPS13",
                    Name = "Dell XPS 13",
                    Description = "13.4” UHD+, i7, 32GB RAM, 1TB SSD, touch",
                    MinPrice = 1799,
                    MaxPrice = 2299,
                    ImageUrl = "https://i.dell.com/sites/csimages/Video_Imagery/all/xps-13-9310-laptop-touch.png"
                },
                new ProductDTO {
                    Sku = "LEN-T14S",
                    Name = "Lenovo ThinkPad T14s",
                    Description = "14” Ryzen 7, 32GB RAM, 1TB SSD, enterprise build",
                    MinPrice = 1599,
                    MaxPrice = 2099,
                    ImageUrl = "https://www.lenovo.com/medias/lenovo-laptop-thinkpad-t14s-gen-2-subseries-hero.png?context=bWFzdGVyfHJvb3R8NzA4ODR8aW1hZ2UvcG5nfGg0NS9oZmEvMTE4NzAyNTQ4NDgxNDIucG5nfDI0ZGY1NmM5YmY4NmYyZGU2YzQ2YTVlY2Y3MjRjNDI3YmJhNDI2NDQzZDFkZDdlNTIxZjI3NzU1YjM2YmIzZTc"
                },
                new ProductDTO {
                    Sku = "LEN-X1C10",
                    Name = "Lenovo ThinkPad X1 Carbon G10",
                    Description = "14” i7, 32GB RAM, 1TB SSD, ultralight premium",
                    MinPrice = 2099,
                    MaxPrice = 2599,
                    ImageUrl = "https://www.lenovo.com/medias/lenovo-laptop-thinkpad-x1-carbon-gen-10-subseries-hero.png?context=bWFzdGVyfHJvb3R8OTczM3xpbWFnZS9wbmd8aGVmL2g3Mi8xMzM5ODc3MzM1NjA0Ni5wbmd8NjUwMGFlYmM2ZjgxYTkwYjgxOGYwNGQwNmFiZjI1MDMwNDI4MDA1ZWIwNmE2NzIzNjM4NTJlNWRjODUxZjhhYg"
                },
                new ProductDTO {
                    Sku = "HP-ELITE840",
                    Name = "HP EliteBook 840 G10",
                    Description = "14” i5, 32GB RAM, 1TB SSD, ultrabook",
                    MinPrice = 1599,
                    MaxPrice = 1999,
                    ImageUrl = "https://ssl-product-images.www8-hp.com/digmedialib/prodimg/lowres/c06217537.png"
                },
                new ProductDTO {
                    Sku = "SURF-LAP-STUDIO2",
                    Name = "Surface Laptop Studio 2",
                    Description = "14.4” PixelSense touchscreen, Intel i7, 32GB RAM, 1TB SSD, NVIDIA RTX",
                    MinPrice = 2599,
                    MaxPrice = 3299,
                    ImageUrl = "https://cdn-dynmedia-1.microsoft.com/is/image/microsoftcorp/Surface-Laptop-Studio-2-1?wid=1200&hei=675&fit=crop"
                },
                new ProductDTO {
                    Sku = "SURF-PRO9",
                    Name = "Surface Pro 9 Tablet",
                    Description = "13” tablet, i7, 32GB RAM, 1TB SSD, detachable keyboard",
                    MinPrice = 1599,
                    MaxPrice = 1999,
                    ImageUrl = "https://cdn.mos.cms.futurecdn.net/2e8Qb9QxgvH5YgJVf4DYYn.jpg"
                },
                new ProductDTO {
                    Sku = "ASUS-EXPERT",
                    Name = "ASUS ExpertBook B9",
                    Description = "14” i7, 32GB RAM, 1TB SSD, long battery life",
                    MinPrice = 1699,
                    MaxPrice = 2099,
                    ImageUrl = "https://dlcdnwebimgs.asus.com/gain/2C7CA934-63E8-4CA7-BE6F-83DCD3D6FE17/w717/h525"
                },
                new ProductDTO {
                    Sku = "ACER-TMP6",
                    Name = "Acer TravelMate P6",
                    Description = "14” FHD IPS, Intel i7, 32GB RAM, 1TB SSD, ultra-light business laptop",
                    MinPrice = 1599,
                    MaxPrice = 1999,
                    ImageUrl = "https://static.acer.com/up/Resource/Acer/Laptops/TravelMate_P6/Image/20240118/TravelMate_P6-2024_Series-main.png"
                }
            };
        }

        public ProductDTO GetBySku(string sku) =>
            _products.FirstOrDefault(p => string.Equals(p.Sku, sku, StringComparison.OrdinalIgnoreCase));

        public List<ProductDTO> GetBySkus(IEnumerable<string> skus) =>
            _products.Where(p => skus.Contains(p.Sku, StringComparer.OrdinalIgnoreCase)).ToList();

        public List<ProductDTO> GetAll() => _products.ToList();
    }
}
