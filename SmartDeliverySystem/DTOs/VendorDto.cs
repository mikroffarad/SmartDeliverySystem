using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.DTOs
{
    public class VendorDto
    {
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class VendorWithProductsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public List<Product> Products { get; set; } = new();
    }
}
