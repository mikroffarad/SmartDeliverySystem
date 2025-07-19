using System.ComponentModel.DataAnnotations;

namespace SmartDeliverySystem.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public decimal Weight { get; set; }

        public string Category { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }
    }
}
