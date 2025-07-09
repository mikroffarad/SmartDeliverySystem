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

    public class LocationHistoryDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
