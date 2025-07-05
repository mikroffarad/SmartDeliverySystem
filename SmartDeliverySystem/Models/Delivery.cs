namespace SmartDeliverySystem.Models
{
    public enum DeliveryStatus
    {
        PendingPayment, // Очікується оплата
        Paid,           // Оплачено
        Assigned,       // Водій призначений
        InTransit,      // В дорозі
        Delivered,      // Доставлено
        Cancelled       // Скасовано
    }

    public class Delivery
    {
        public int Id { get; set; }

        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public int StoreId { get; set; }
        public Store? Store { get; set; }

        public List<DeliveryProduct> Products { get; set; } = new();

        public DeliveryStatus Status { get; set; } = DeliveryStatus.PendingPayment;

        public string? DriverId { get; set; }

        public string? GpsTrackerId { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AssignedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }
        public DateTime PaymentDate { get; internal set; }
        public string? PaymentMethod { get; internal set; }
        public decimal PaidAmount { get; internal set; }
        public double? FromLatitude { get; set; }
        public double? FromLongitude { get; set; }
        public double? ToLatitude { get; set; }
        public double? ToLongitude { get; set; }

        // GPS Tracking properties
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        public string? TrackingNotes { get; set; }
    }
    public class DeliveryProduct
    {
        public int Id { get; set; }
        public int DeliveryId { get; set; }
        public Delivery? Delivery { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
    }

    // New GPS tracking model
    public class DeliveryLocationHistory
    {
        public int Id { get; set; }
        public int DeliveryId { get; set; }
        public Delivery? Delivery { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }
        public double? Speed { get; set; } // km/h
    }
}
