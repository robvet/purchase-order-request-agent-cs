namespace NearbyCS_API.Models.DTO
{
   public class ProductDTO
   {
      public string Sku { get; set; }
      public string Name { get; set; }
      public string Description { get; set; }
      public decimal Cost { get; set; }
      public bool IsAvailable { get; set; } // Indicates if the product is currently available for purchase
      public string ImageUrl { get; set; }
      public BaseSpecsDTO BaseSpecs { get; set; }
      public List<UpgradeOptionDTO> UpgradeOptions { get; set; }
   }

    public class BaseSpecsDTO
   {
      public string Ram { get; set; }
      public string Storage { get; set; }
      public string Cpu { get; set; }
   }

   public class UpgradeOptionDTO
   {
      public string Type { get; set; }      // e.g. "ram", "storage", "cpu"
      public string To { get; set; }        // e.g. "32GB", "2TB SSD", etc.
      public int CostDelta { get; set; }    // The price difference for the upgrade
   }
}
