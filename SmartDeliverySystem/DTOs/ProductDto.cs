namespace SmartDeliverySystem.DTOs
{
    public class ProductDto
    {
        public string Name { get; set; }
        public decimal Weight { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int VendorId { get; set; }
    }
}
