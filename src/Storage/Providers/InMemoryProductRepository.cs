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
                    Description = "16-inch MacBook Pro with Apple M3 Pro chip",
                    MinPrice = 2799,
                    MaxPrice = 3899,
                    ImageUrl = "https://example.com/images/mbp16-m3.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "M3 Pro"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "M3 Max (High Performance)", CostDelta = 220 }
                    }
                },
                new ProductDTO {
                    Sku = "MBP-14-M3",
                    Name = "MacBook Pro 14” (M3 Pro)",
                    Description = "14-inch MacBook Pro with Apple M3 Pro chip",
                    MinPrice = 1999,
                    MaxPrice = 3099,
                    ImageUrl = "https://example.com/images/mbp14-m3.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "M3 Pro"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "M3 Max (High Performance)", CostDelta = 220 }
                    }
                },
                new ProductDTO {
                    Sku = "DELL-LAT5440",
                    Name = "Dell Latitude 5440",
                    Description = "14-inch Dell Latitude laptop with Intel Core i7 processor",
                    MinPrice = 1450,
                    MaxPrice = 2150,
                    ImageUrl = "https://example.com/images/dell-lat5440.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "i7-1355U"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "i7-1370P (High Performance)", CostDelta = 220 }
                    }
                },
                new ProductDTO {
                    Sku = "DELL-XPS13",
                    Name = "Dell XPS 13",
                    Description = "Premium 13.4-inch Dell XPS laptop with InfinityEdge display",
                    MinPrice = 1299,
                    MaxPrice = 1899,
                    ImageUrl = "https://example.com/images/dell-xps13.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "i7-1355U"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "i7-1370P (High Performance)", CostDelta = 220 }
                    }
                },
                new ProductDTO {
                    Sku = "LEN-T14S",
                    Name = "Lenovo ThinkPad T14s",
                    Description = "Lightweight 14-inch Lenovo ThinkPad with Intel Core i7",
                    MinPrice = 1380,
                    MaxPrice = 2100,
                    ImageUrl = "https://example.com/images/len-t14s.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "i7-1355U"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "i7-1370P (High Performance)", CostDelta = 220 }
                    }
                },
                new ProductDTO {
                    Sku = "LEN-X1C10",
                    Name = "Lenovo ThinkPad X1 Carbon G10",
                    Description = "Ultra-light 14-inch X1 Carbon Gen 10 with Intel Core i7",
                    MinPrice = 1720,
                    MaxPrice = 2499,
                    ImageUrl = "https://example.com/images/len-x1c10.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "i7-1355U"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "i7-1370P (High Performance)", CostDelta = 220 }
                    }
                },
                new ProductDTO {
                    Sku = "HP-ELITE840",
                    Name = "HP EliteBook 840 G10",
                    Description = "14-inch HP EliteBook with enterprise security and Intel Core i7",
                    MinPrice = 1580,
                    MaxPrice = 2399,
                    ImageUrl = "https://example.com/images/hp-elite840.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "i7-1355U"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "i7-1370P (High Performance)", CostDelta = 220 }
                    }
                },
                new ProductDTO {
                    Sku = "SURF-LAP-STUDIO2",
                    Name = "Surface Laptop Studio 2",
                    Description = "Versatile 14.4-inch Surface Laptop Studio 2 with Intel Core i7",
                    MinPrice = 1999,
                    MaxPrice = 3199,
                    ImageUrl = "https://example.com/images/surf-lap-studio2.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "i7-13700H"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "i9-13900H (High Performance)", CostDelta = 220 }
                    }
                },
                new ProductDTO {
                    Sku = "SURF-PRO9",
                    Name = "Surface Pro 9 Tablet",
                    Description = "13-inch Surface Pro 9 2-in-1 tablet with Intel Core i7",
                    MinPrice = 1399,
                    MaxPrice = 1999,
                    ImageUrl = "https://example.com/images/surf-pro9.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "i7-1255U"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "i7-1265U (High Performance)", CostDelta = 220 }
                    }
                },
                new ProductDTO {
                    Sku = "ASUS-EXPERT",
                    Name = "ASUS ExpertBook B9",
                    Description = "14-inch ASUS ExpertBook B9 with Intel Core i7 and ultra-light chassis",
                    MinPrice = 1520,
                    MaxPrice = 2149,
                    ImageUrl = "https://example.com/images/asus-expert.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "i7-1355U"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "i7-1370P (High Performance)", CostDelta = 220 }
                    }
                },
                new ProductDTO {
                    Sku = "ACER-TMP6",
                    Name = "Acer TravelMate P6",
                    Description = "14-inch Acer TravelMate P6 with Intel Core i7 for business professionals",
                    MinPrice = 1360,
                    MaxPrice = 1980,
                    ImageUrl = "https://example.com/images/acer-tmp6.jpg",
                    BaseSpecs = new BaseSpecsDTO {
                        Ram = "16GB",
                        Storage = "512GB SSD",
                        Cpu = "i7-1355U"
                    },
                    UpgradeOptions = new List<UpgradeOptionDTO> {
                        new UpgradeOptionDTO { Type = "ram", To = "32GB", CostDelta = 180 },
                        new UpgradeOptionDTO { Type = "ram", To = "64GB", CostDelta = 380 },
                        new UpgradeOptionDTO { Type = "storage", To = "1TB SSD", CostDelta = 140 },
                        new UpgradeOptionDTO { Type = "storage", To = "2TB SSD", CostDelta = 300 },
                        new UpgradeOptionDTO { Type = "cpu", To = "i7-1370P (High Performance)", CostDelta = 220 }
                    }
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
