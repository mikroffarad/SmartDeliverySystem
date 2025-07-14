using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Key]
        public int Id { get; set; }

        [ForeignKey("Vendor")]
        public int VendorId { get; set; }

        [ForeignKey("Store")]
        public int StoreId { get; set; }

        public decimal TotalAmount { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaymentDate { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? PaidAmount { get; set; }
        public DateTime? AssignedAt { get; set; }
        public string? DriverId { get; set; }
        public string? GpsTrackerId { get; set; }

        // Location coordinates
        public double? FromLatitude { get; set; }
        public double? FromLongitude { get; set; }
        public double? ToLatitude { get; set; }
        public double? ToLongitude { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? TrackingNotes { get; set; }

        // Navigation properties
        public virtual Vendor Vendor { get; set; } = null!;
        public virtual Store Store { get; set; } = null!;
        public virtual ICollection<DeliveryProduct> Products { get; set; } = new List<DeliveryProduct>();
        public virtual ICollection<DeliveryLocationHistory> LocationHistory { get; set; } = new List<DeliveryLocationHistory>();
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
