using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.DTOs
{
    public class LocationUpdateDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public string? Notes { get; set; }
    }

    public class LocationUpdateServiceBusDto
    {
        public int DeliveryId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class RouteIndexDto
    {
        public int Index { get; set; }
    }
}
