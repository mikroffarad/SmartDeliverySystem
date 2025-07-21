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

    public class LocationUpdateMessage
    {
        public int DeliveryId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Speed { get; set; }
        public string? Notes { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Helper classes for working with routes
    public class RouteSimulationDto
    {
        public List<RoutePointDto> RoutePoints { get; set; } = new();
        public int CurrentIndex { get; set; }
        public DateTime StartTime { get; set; }
    }

    public class RoutePointDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    // DTOs for OSRM response deserialization
    public class OSRMResponseDto
    {
        public OSRMRouteDto[]? routes { get; set; }
    }

    public class OSRMRouteDto
    {
        public OSRMGeometryDto? geometry { get; set; }
        public double distance { get; set; }
        public double duration { get; set; }
    }
    
    public class OSRMGeometryDto
    {
        public double[][]? coordinates { get; set; }
    }
}
