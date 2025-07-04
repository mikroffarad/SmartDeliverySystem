using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartDeliverySystem.Models
{
    public class Vendor
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        [JsonIgnore]
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
