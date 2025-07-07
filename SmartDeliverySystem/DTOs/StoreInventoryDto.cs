namespace SmartDeliverySystem.DTOs
{
    public class StoreInventoryDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Weight { get; set; }
    }

    public class AddToInventoryDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
