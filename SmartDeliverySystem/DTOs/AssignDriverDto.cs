
using SmartDeliverySystem.Models;

namespace SmartDeliverySystem.DTOs
{
    public class AssignDriverDto
    {
        public string DriverId { get; set; } = string.Empty;
        public string GpsTrackerId { get; set; } = string.Empty;
        public DeliveryType DeliveryType { get; set; }
        public double? FromLatitude { get; set; }
        public double? FromLongitude { get; set; }
        public double? ToLatitude { get; set; }
        public double? ToLongitude { get; set; }
    }
}
