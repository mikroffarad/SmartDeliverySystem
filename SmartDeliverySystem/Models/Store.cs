using System.ComponentModel.DataAnnotations;

namespace SmartDeliverySystem.Models
{
    public class Store
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }

    public class StoreProduct
    {
        public int StoreId { get; set; }
        public Store? Store { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
    }
}
