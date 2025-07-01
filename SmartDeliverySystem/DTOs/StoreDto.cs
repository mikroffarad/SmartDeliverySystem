namespace SmartDeliverySystem.DTOs
{
    public class StoreDto
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
