namespace SmartDeliverySystem.Azure.Functions.DTOs
{
    public class DeliveryTrackingData
    {
        public int DeliveryId { get; set; }
        public int Status { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        public double? VendorLatitude { get; set; }
        public double? VendorLongitude { get; set; }
        public double? StoreLatitude { get; set; }
        public double? StoreLongitude { get; set; }
    }
}
