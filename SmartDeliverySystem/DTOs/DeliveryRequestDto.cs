namespace SmartDeliverySystem.DTOs
{
    public class DeliveryRequestDto
    {
        public int VendorId { get; set; }
        public List<ProductRequestDto> Products { get; set; } = new();
    }

    public class ProductRequestDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class DeliveryResponseDto
    {
        public int DeliveryId { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public string StoreAddress { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string EstimatedDeliveryTime { get; set; } = string.Empty;
    }
}