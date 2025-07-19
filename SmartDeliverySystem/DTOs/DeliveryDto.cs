using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.DTOs
{
    public class DeliveryDto
    {
        public int Id { get; set; }
        public int DeliveryId { get; set; }
        public int VendorId { get; set; }
        public int StoreId { get; set; }
        public string? VendorName { get; set; }
        public string? StoreName { get; set; }
        public decimal TotalAmount { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public string? DriverId { get; set; }
        public string? GpsTrackerId { get; set; }
        public double? FromLatitude { get; set; }
        public double? FromLongitude { get; set; }
        public double? ToLatitude { get; set; }
        public double? ToLongitude { get; set; }
        public double? StoreLatitude { get; set; }
        public double? StoreLongitude { get; set; }
        public double? VendorLatitude { get; set; }
        public double? VendorLongitude { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
    }
}
