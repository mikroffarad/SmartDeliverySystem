using SmartDeliverySystem.Models;
using System;
using System.Collections.Generic;

namespace SmartDeliverySystem.DTOs
{
    public class LocationHistoryDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Notes { get; set; }
        public double? Speed { get; set; }
    }

    public class DeliveryTrackingDto
    {
        public int DeliveryId { get; set; }
        public string DriverId { get; set; } = string.Empty;
        public string GpsTrackerId { get; set; } = string.Empty;
        public DeliveryStatus Status { get; set; }
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
        public DateTime? LastLocationUpdate { get; set; }
        public double? FromLatitude { get; set; }
        public double? FromLongitude { get; set; }
        public double? ToLatitude { get; set; }
        public double? ToLongitude { get; set; }
        public double? VendorLatitude { get; set; }
        public double? VendorLongitude { get; set; }
        public double? StoreLatitude { get; set; }
        public double? StoreLongitude { get; set; }
        public List<LocationHistoryDto> LocationHistory { get; set; } = new();
    }
}
