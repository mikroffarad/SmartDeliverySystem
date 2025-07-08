namespace SmartDeliverySystem.DTOs
{
    public class DeliveryResponseDto
    {
        public int DeliveryId { get; set; }
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class FindBestStoreResponseDto
    {
        public int StoreId { get; set; }
        public string StoreName { get; set; } = string.Empty;
        public double Distance { get; set; }
    }

    public class FindBestStoreRequestDto
    {
        public int VendorId { get; set; }
        public List<ProductRequestDto> Products { get; set; } = new List<ProductRequestDto>();
    }
}
