using System.ComponentModel.DataAnnotations;

namespace SmartDeliverySystem.Models
{
    public class Vendor
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string ContactEmail { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}
