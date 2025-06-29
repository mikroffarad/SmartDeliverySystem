namespace SmartDeliverySystem.Models
{
    public enum DeliveryStatus
    {
        Pending,
        Assigned,
        InTransit,
        Delivered,
        Cancelled,
        Paid
    }

    public enum DeliveryType
    {
        Standard,
        Express,
        SameDay
    }

    public class Delivery
    {
        public int Id { get; set; }

        public int VendorId { get; set; }
        public Vendor? Vendor { get; set; }

        public int StoreId { get; set; }
        public Store? Store { get; set; }

        public List<DeliveryProduct> Products { get; set; } = new();

        public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;

        public DeliveryType Type { get; set; } = DeliveryType.Standard;

        public string? DriverId { get; set; }

        public string? GpsTrackerId { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? AssignedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }
        public DateTime PaymentDate { get; internal set; }
        public string? PaymentMethod { get; internal set; }
        public decimal PaidAmount { get; internal set; }
    }

    public class DeliveryProduct
    {
        public int Id { get; set; }
        public int DeliveryId { get; set; }
        public Delivery Delivery { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
    }
}
