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
                    Cost = (2799 + 3899) / 2m,
                    ImageUrl = "https://example.com/images/mbp16-m3.jpg",
                    IsAvailable = true,
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
                    Cost = (1999 + 3099) / 2m,
                    ImageUrl = "https://example.com/images/mbp14-m3.jpg",
                    IsAvailable = true,
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
                    Cost = (1450 + 2150) / 2m,
                    ImageUrl = "https://example.com/images/dell-lat5440.jpg",
                    IsAvailable = true,
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
                    Cost = (1299 + 1899) / 2m,
                    ImageUrl = "https://example.com/images/dell-xps13.jpg",
                    IsAvailable = true,
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
                    Cost = (1380 + 2100) / 2m,
                    ImageUrl = "https://example.com/images/len-t14s.jpg",
                    IsAvailable = true,
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
                    Cost = (1720 + 2499) / 2m,
                    ImageUrl = "https://example.com/images/len-x1c10.jpg",
                    IsAvailable = true,
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
                    Cost = (1580 + 2399) / 2m,
                    ImageUrl = "https://example.com/images/hp-elite840.jpg",
                    IsAvailable = true,
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
                    Cost = (1999 + 3199) / 2m,
                    ImageUrl = "https://example.com/images/surf-lap-studio2.jpg",
                    IsAvailable = true,
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
                    Cost = (1399 + 1999) / 2m,
                    ImageUrl = "https://example.com/images/surf-pro9.jpg",
                    IsAvailable = true,
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
                    Cost = (1520 + 2149) / 2m,
                    ImageUrl = "https://example.com/images/asus-expert.jpg",
                    IsAvailable = false,
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
                    Cost = (1360 + 1980) / 2m,
                    ImageUrl = "https://example.com/images/acer-tmp6.jpg",
                    IsAvailable = true,
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

        public Task<ProductDTO?> GetBySku(string sku) =>
            Task.FromResult(_products.Where(p => string.Equals(p.Sku, sku, StringComparison.OrdinalIgnoreCase))
            .Select(p => new ProductDTO
            {
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                Cost = p.Cost,
                ImageUrl = p.ImageUrl
            }).FirstOrDefault());

        public Task<List<ProductDTO>> GetBySkus(IEnumerable<string> skus) =>
            Task.FromResult(_products.Where(p => skus.Contains(p.Sku, StringComparer.OrdinalIgnoreCase))
            .Select(p => new ProductDTO
            {
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                Cost = p.Cost
            }).ToList());

        public Task<List<ProductDTO>> GetAllProductsSummaryViewAsync() => 
            Task.FromResult(_products.Select(p => new ProductDTO
        {
            Sku = p.Sku,
            Name = p.Name,
            Description = p.Description,
            Cost = p.Cost
        }).ToList());

        public Task<List<ProductDTO>> GetAlternativeProductsAsync() =>
           Task.FromResult(_products.Select(p => new ProductDTO
           {
               Sku = p.Sku,
               Name = p.Name,
               Description = p.Description,
               Cost = p.Cost,
               BaseSpecs = p.BaseSpecs,
           }).Where(p => p.IsAvailable)
           .ToList());
    }
}
